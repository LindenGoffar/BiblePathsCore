using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class BibleChapter
    {

        [NotMapped]
        public int QuestionCount { get; set; }
        [NotMapped]
        public int FITBQuestionCount { get; set; }
        [NotMapped]
        public int FITBPct { get; set; }
        [NotMapped]
        public bool IsCommentary { get; set; }
        [NotMapped]
        public bool HasChallenge { get; set; }


        public bool AddPBEChapterProperties(List<QuizQuestion> Questions)
        {
            QuestionCount = GetQuestionCount(Questions);
            FITBQuestionCount = GetFITBQuestionCount(Questions);
            FITBPct = GetFITBPctWithQuestionList(Questions);
            HasChallenge = HasChallengedQuestion(Questions);
            IsCommentary = (ChapterNumber == Bible.CommentaryChapter);
            return true;
        }

        public int GetQuestionCount(List<QuizQuestion> Questions)
        {
            return Questions.Where(Q => Q.BookNumber == BookNumber 
                                && Q.Chapter == ChapterNumber 
                                && (Q.BibleId == BibleId || Q.BibleId == null)
                                && Q.IsDeleted == false)
                        .Count();
        }

        public int GetFITBQuestionCount(List<QuizQuestion> Questions)
        {
            return Questions.Where(Q => Q.BookNumber == BookNumber
                                && Q.Chapter == ChapterNumber
                                && (Q.BibleId == BibleId || Q.BibleId == null)
                                && Q.Type == (int)QuestionType.FITB
                                && Q.IsDeleted == false)
                        .Count();
        }

        public int GetFITBPctWithQuestionList(List<QuizQuestion> Questions)
        {
            if (QuestionCount > 0 && FITBQuestionCount > 0)
            {
                return (FITBQuestionCount / QuestionCount) * 100;
            }
            return 0;
        }

        public bool HasChallengedQuestion(List<QuizQuestion> Questions)
        {
            return Questions.Where(Q => Q.BookNumber == BookNumber
                                    && Q.Chapter == ChapterNumber
                                    && (Q.BibleId == BibleId || Q.BibleId == null)
                                    && Q.IsDeleted == false
                                    && Q.Challenged == true)
                            .Any();
        }

        public static async Task<int> GetVerseCountAsync(BiblePathsCoreDbContext context, string BibleId, int BookNumber, int ChapterNumber)
        {
            return await context.BibleChapters
                .Where(C => C.BibleId == BibleId && C.BookNumber == BookNumber && C.ChapterNumber == ChapterNumber)
                .Select(C => C.Verses ?? 0)
                .FirstOrDefaultAsync();
        }
        public async Task<string> GetValidBibleIdAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            string RetVal = Bible.DefaultPBEBibleId;
            if (BibleId != null)
            {
                if (await context.Bibles.Where(B => B.Id == BibleId).AnyAsync())
                {
                    RetVal = BibleId;
                }
            }
            return RetVal;
        }

    }
    public class MinChapter
    {
        public string BibleId { get; set; }
        public int BookNumber { get; set; }
        public string Name { get; set; }
        public int ChapterNumber { get; set; }
        public int? Verses { get; set; }

        public MinChapter()
        {

        }
        public MinChapter(BibleChapter Chapter)
        {
            BibleId = Chapter.BibleId;
            BookNumber = Chapter.BookNumber;
            Name = Chapter.Name;
            ChapterNumber = Chapter.ChapterNumber;
            Verses = Chapter.Verses ?? 0;
        }
    }
}
