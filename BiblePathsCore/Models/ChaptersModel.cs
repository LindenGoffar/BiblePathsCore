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
        public bool IsCommentary { get; set; }
        [NotMapped]
        public bool HasChallenge { get; set; }

        public bool AddPBEChapterProperties(List<QuizQuestion> Questions)
        {
            QuestionCount = GetQuestionCount(Questions);
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

        public bool HasChallengedQuestion(List<QuizQuestion> Questions)
        {
            return Questions.Where(Q => Q.BookNumber == BookNumber
                                    && Q.Chapter == ChapterNumber
                                    && (Q.BibleId == BibleId || Q.BibleId == null)
                                    && Q.IsDeleted == false
                                    && Q.Challenged == true)
                            .Any();
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

}
