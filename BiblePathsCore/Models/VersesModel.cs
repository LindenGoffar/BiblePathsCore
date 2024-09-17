using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace BiblePathsCore.Models.DB
{
    public partial class BibleVerse
    {
        [NotMapped]
        public bool InPath { get; set; }
        [NotMapped]
        public int Proximity { get; set; }
        [NotMapped]
        public int QuestionCount { get; set; }
        [NotMapped]
        public bool IsPBEExcluded { get; set; }
        [NotMapped]
        public bool InRelatedPaths { get; set; }
        [NotMapped]
        public List<Path> RelatedPaths { get; set;  }
        [NotMapped]
        public int FITBPct { get; set; }
        [NotMapped]
        public string SectionTitle { get; set; } // in the Commentary Scenario a verse is a Section.

        // This one is expensive we shouldn't use this one if we can avoid.
        //public async Task<int> GetQuestionCountAsync(BiblePathsCoreDbContext context)
        //{
        //    return await context.QuizQuestions
        //                .Where(Q => Q.BookNumber == BookNumber 
        //                        && Q.Chapter == Chapter 
        //                        && Q.EndVerse == Verse 
        //                        && (Q.BibleId == BibleId || Q.BibleId == null)
        //                        && Q.IsDeleted == false
        //                        && Q.Type == (int)QuestionType.Standard)
        //                .CountAsync();
        //}

        public int GetQuestionCountWithQuestionList(List<QuizQuestion> Questions)
        {
            return Questions
                        .Where(Q => Q.BookNumber == BookNumber
                                && Q.Chapter == Chapter
                                && Q.EndVerse == Verse
                                && (Q.BibleId == BibleId || Q.BibleId == null)
                                && Q.IsDeleted == false
                                && (Q.Type == (int)QuestionType.Standard
                                    || Q.Type == (int)QuestionType.FITB))
                        .Count();
        }

        public int GetFITBPctWithQuestionList(List<QuizQuestion> Questions)
        {
            int QuestionCount = Questions.Where(Q => Q.BookNumber == BookNumber
                                                && Q.Chapter == Chapter
                                                && Q.EndVerse == Verse
                                                && (Q.BibleId == BibleId || Q.BibleId == null)
                                                && Q.IsDeleted == false
                                                && (Q.Type == (int)QuestionType.Standard
                                                    || Q.Type == (int)QuestionType.FITB))
                                       .Count();
            int FITBCount = Questions.Where(Q => Q.BookNumber == BookNumber
                                                && Q.Chapter == Chapter
                                                && Q.EndVerse == Verse
                                                && (Q.BibleId == BibleId || Q.BibleId == null)
                                                && Q.IsDeleted == false
                                                && Q.Type == (int)QuestionType.FITB)
                                        .Count();
            if (QuestionCount > 0)
            {
                return (FITBCount / QuestionCount) * 100;
            }
            return 0;
        }

        public bool IsVerseInExclusionList(List<QuizQuestion> ExclusionQuestions)
        {
            bool RetVal = false;
            RetVal =  ExclusionQuestions.Any(E => E.BookNumber == BookNumber
                                           && E.Chapter == Chapter
                                           && E.Type == (int)QuestionType.Exclusion
                                           && E.IsDeleted == false
                                           && E.StartVerse <= Verse
                                           && E.EndVerse >= Verse
                                           );
            return RetVal;
        }

        public static async Task<BibleVerse> GetVerseAsync(BiblePathsCoreDbContext context, string BibleId, int BookNumber, int Chapter, int Verse)
        {
            BibleVerse bibleVerse = new BibleVerse();
            // this seems an odd fit in the VersesModel but we need to 
            // try and handle both the Bible Verse scenario as well as the
            // Commentary Section model which presents as a Verse. 
            if (Chapter != Bible.CommentaryChapter) // This is the Bible Verse Scenario
            {
                try
                {
                    bibleVerse = await context.BibleVerses.Where(v => v.BibleId == BibleId
                                                                    && v.BookNumber == BookNumber
                                                                    && v.Chapter == Chapter
                                                                    && v.Verse == Verse)
                                                                .SingleAsync();
                }
                catch
                {
                    return null;
                }
            }
            else // The Commentary Section scenario
            {
                try
                {
                    CommentaryBook commentarySection = await context.CommentaryBooks.Where(c => c.BibleId == BibleId
                                                                                            && c.BookNumber == BookNumber
                                                                                            && c.SectionNumber == Verse)
                                                                                    .FirstOrDefaultAsync();
                    bibleVerse.BibleId = BibleId;
                    bibleVerse.BookNumber = BookNumber;
                    bibleVerse.Chapter = Chapter;
                    bibleVerse.Verse = commentarySection.SectionNumber;
                    bibleVerse.SectionTitle = commentarySection.SectionTitle;
                    bibleVerse.Text = commentarySection.Text;
                }
                catch
                {
                    return null;
                }
            }
            return bibleVerse;
        }

        public async Task<bool> GetRelatedPathsAsync(BiblePathsCoreDbContext context)
        {
            InRelatedPaths = false;
            List<Path> relatedPaths = new List<Path>();
            // Return all PathNodes or Steps that contain this verse.
            // EF CORE 5 Will contain Filtered Includes we can simplify when it releases. 
            List<PathNode> RelatedSteps = await context.PathNodes
                                                        .Include(N => N.Path)
                                                        .Where(N => N.BookNumber == BookNumber
                                                                && N.Chapter == Chapter
                                                                && N.StartVerse <= Verse
                                                                && N.EndVerse >= Verse)
                                                        .ToListAsync();
            foreach (PathNode relatedStep in RelatedSteps)
            {
                if (relatedStep.Path.IsDeleted == false && relatedStep.Path.IsPublished == true)
                {
                    InRelatedPaths = true;
                    // We only add a path object on the EndVerse, and only if it hasn'be already been added. 
                    if (relatedStep.EndVerse == Verse && relatedPaths.Contains(relatedStep.Path) == false)
                    {
                        _ = await relatedStep.Path.AddCalculatedPropertiesAsync(context);
                        relatedPaths.Add(relatedStep.Path);
                    }
                }
            }
            RelatedPaths = relatedPaths;
            return true;                                                                    
        }
    }
}
