using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class BibleBooks
    {
        [NotMapped]
        public bool InBookList { get; set; }
        [NotMapped]
        public int QuestionCount { get; set; }
        [NotMapped]
        public bool HasCommentary { get; set; }
        [NotMapped]
        public string CommentaryTitle { get; set; }
        [NotMapped]
        public int CommentaryQuestionCount { get; set; }

        public static async Task<BibleBooks> GetPBEBookAndChapterAsync(BiblePathsCoreDbContext context, string BibleId, int BookNumber, int ChapterNum)
        {
            BibleBooks PBEBook = await context.BibleBooks
                                                  .Where(B => B.BibleId == BibleId && B.BookNumber == BookNumber)
                                                  .SingleAsync();
            if (PBEBook == null) { return null; }
            await PBEBook.AddPBEBookPropertiesAsync(context, ChapterNum);
            return PBEBook;
        }
        public static async Task<IList<BibleBooks>> GetPBEBooksAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            IList<BibleBooks> PBEBooks = await context.BibleBooks
                                                  .Include(B => B.BibleChapters)
                                                  .Where(B => B.BibleId == BibleId)
                                                  .ToListAsync();
            foreach (BibleBooks Book in PBEBooks)
            {
                await Book.AddPBEBookPropertiesAsync(context, null);
            }
            return PBEBooks;
        }
        public async Task<bool> AddPBEBookPropertiesAsync(BiblePathsCoreDbContext context, int? ChapterNum)
        {
            InBookList = await IsInBooklistAsync(context);
            QuestionCount = await GetQuestionCountAsync(context);
            HasCommentary = await HasCommentaryAsync(context);
            if (HasCommentary)
            {
                CommentaryTitle = await GetCommentaryTitleAsync(context);
                CommentaryQuestionCount = await GetCommentaryQuestionCountAsync(context);
            }
            if (ChapterNum.HasValue)
            {
                BibleChapters Chapter = await context.BibleChapters
                                                        .Where(C => C.BibleId == BibleId && C.BookNumber == BookNumber && C.ChapterNumber == ChapterNum)
                                                        .SingleAsync();
                await Chapter.AddPBEChapterPropertiesAsync(context);
                this.BibleChapters.Add(Chapter);
            }
            else
            {
                // Caller must have loaded Chapters
                foreach (BibleChapters Chapter in BibleChapters)
                {
                    await Chapter.AddPBEChapterPropertiesAsync(context);
                }
            }
            return true;
        }

        public async Task<bool> IsInBooklistAsync(BiblePathsCoreDbContext context)
        {
            // We need to determine whether any of the non-deleted Book Maps contain the book.
            // Then we need to confirm that the BookList itself is not deleted. 
            // TODO: This is not ideal, we should be simply deleting rather than soft deleting these
            //       So that a simple ANY would work. 
            List<QuizBookListBookMap> BookMapList = await context.QuizBookListBookMap
                                                .Include(M => M.BookList)
                                                .Where(M => M.BookNumber == BookNumber && M.IsDeleted == false)
                                                .ToListAsync();

            foreach (QuizBookListBookMap bookMap in BookMapList)
            {
                if (bookMap.BookList.IsDeleted != false)
                {
                    // We can return now with true we found one.
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> HasCommentaryAsync(BiblePathsCoreDbContext context)
        {
            return await context.CommentaryBooks
                             .Where(C => C.BibleId == BibleId && C.BookNumber == BookNumber)
                             .AnyAsync();
        }

        public async Task<int> GetQuestionCountAsync(BiblePathsCoreDbContext context)
        {
            return await context.QuizQuestions
                        .Where(Q => Q.BookNumber == BookNumber && Q.BibleId == BibleId && Q.IsDeleted == false)
                        .CountAsync();
        }
        public async Task<int> GetCommentaryQuestionCountAsync(BiblePathsCoreDbContext context)
        {
            return await context.QuizQuestions
                        .Where(Q => Q.BookNumber == BookNumber && Q.BibleId == BibleId && Q.Chapter == Bibles.CommentaryChapter && Q.IsDeleted == false)
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

        public async Task<string> GetCommentaryTitleAsync(BiblePathsCoreDbContext context)
        {
            return (await context.CommentaryBooks
                             .Where(C => C.BibleId == BibleId && C.BookNumber == BookNumber)
                             .FirstAsync()).BookName;
        }


    }  

}
