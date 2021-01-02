using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

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
        public bool InRelatedPaths { get; set; }
        [NotMapped]
        public List<Path> RelatedPaths { get; set;  }

        // This one is expensive we shouldn't use this one if we can avoid.
        public async Task<int> GetQuestionCountAsync(BiblePathsCoreDbContext context)
        {
            return await context.QuizQuestions
                        .Where(Q => Q.BookNumber == BookNumber 
                                && Q.Chapter == Chapter 
                                && Q.EndVerse == Verse 
                                && (Q.BibleId == BibleId || Q.BibleId == null)
                                && Q.IsDeleted == false)
                        .CountAsync();
        }

        public int GetQuestionCountWithQuestionList(List<QuizQuestion> Questions)
        {
            return Questions
                        .Where(Q => Q.BookNumber == BookNumber
                                && Q.Chapter == Chapter
                                && Q.EndVerse == Verse
                                && (Q.BibleId == BibleId || Q.BibleId == null)
                                && Q.IsDeleted == false)
                        .Count();
        }

        public static async Task<BibleVerse> GetVerseAsync(BiblePathsCoreDbContext context, string BibleId, int BookNumber, int Chapter, int Verse)
        {
            BibleVerse bibleVerse = new BibleVerse();
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
