using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class BibleChapters
    {

        [NotMapped]
        public int QuestionCount { get; set; }
        [NotMapped]
        public bool IsCommentary { get; set; }

        public bool AddPBEChapterProperties(List<QuizQuestions> Questions)
        {
            QuestionCount = GetQuestionCount(Questions);
            IsCommentary = (ChapterNumber == Bibles.CommentaryChapter);
            return true;
        }

        public int GetQuestionCount(List<QuizQuestions> Questions)
        {
            return Questions.Where(Q => Q.BookNumber == BookNumber 
                                && Q.Chapter == ChapterNumber 
                                && (Q.BibleId == BibleId || Q.BibleId == null)
                                && Q.IsDeleted == false)
                        .Count();
        }

        public async Task<string> GetValidBibleIdAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            string RetVal = Bibles.DefaultPBEBibleId;
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
