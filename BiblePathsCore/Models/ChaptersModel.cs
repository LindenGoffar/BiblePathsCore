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

        public async Task<bool> AddPBEChapterPropertiesAsync(BiblePathsCoreDbContext context)
        {
            QuestionCount = await GetQuestionCountAsync(context);
            IsCommentary = (ChapterNumber == Bibles.CommentaryChapter);
            return true;
        }

        public async Task<int> GetQuestionCountAsync(BiblePathsCoreDbContext context)
        {
            return await context.QuizQuestions
                        .Where(Q => Q.BookNumber == BookNumber 
                                && Q.Chapter == ChapterNumber 
                                && Q.BibleId == BibleId && Q.IsDeleted == false)
                        .CountAsync();
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
