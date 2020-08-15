using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
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

        public static async Task<BibleBooks> GetBookAndChapterByNameAsync(BiblePathsCoreDbContext context, string BibleId, string BookName, int ChapterNum)
        {
            BibleBooks PBEBook = await context.BibleBooks
                                                  .Where(B => B.BibleId == BibleId && B.Name.ToLower().Contains(BookName.ToLower()))
                                                  .SingleAsync();
            if (PBEBook == null) { return null; }
            await PBEBook.AddPBEBookPropertiesAsync(context, ChapterNum, null);
            return PBEBook;
        }

        public static async Task<BibleBooks> GetPBEBookAndChapterAsync(BiblePathsCoreDbContext context, string BibleId, int BookNumber, int ChapterNum)
        {
            BibleBooks PBEBook = await context.BibleBooks
                                                  .Where(B => B.BibleId == BibleId && B.BookNumber == BookNumber)
                                                  .SingleAsync();
            if (PBEBook == null) { return null; }
            await PBEBook.AddPBEBookPropertiesAsync(context, ChapterNum, null);
            return PBEBook;
        }
        public static async Task<IList<BibleBooks>> GetPBEBooksAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            IList<BibleBooks> PBEBooks = await context.BibleBooks
                                                  .Include(B => B.BibleChapters)
                                                  .Where(B => B.BibleId == BibleId)
                                                  .ToListAsync();
            // Querying for Question counts for each Book/Chapter gets expensive let's grab all of them
            // and pass them around for counting.
            List<QuizQuestions> Questions = await context.QuizQuestions
                                                        .Where(Q => Q.BibleId == BibleId 
                                                                && Q.IsDeleted == false)
                                                        .ToListAsync();

            foreach (BibleBooks Book in PBEBooks)
            {
                await Book.AddPBEBookPropertiesAsync(context, null, Questions);
            }
            return PBEBooks;
        }

        public static async Task<List<BibleBooks>> GetPBEBooksWithQuestionsAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            List<BibleBooks> ReturnBooks = new List<BibleBooks>();
            List<BibleBooks> PBEBooks = await context.BibleBooks
                                                  .Where(B => B.BibleId == BibleId)
                                                  .ToListAsync();
            // Querying for Question counts for each Book/Chapter gets expensive let's grab all of them
            // and pass them around for counting.
            List<QuizQuestions> Questions = await context.QuizQuestions
                                                        .Where(Q => Q.BibleId == BibleId
                                                                && Q.IsDeleted == false)
                                                        .ToListAsync();

            foreach (BibleBooks Book in PBEBooks)
            {
                Book.QuestionCount = Book.GetQuestionCount(Questions);
                if (Book.QuestionCount > 0)
                {
                    ReturnBooks.Add(Book);
                }
            }
            return ReturnBooks;
        }

        public async Task<bool> AddPBEBookPropertiesAsync(BiblePathsCoreDbContext context, int? ChapterNum, List<QuizQuestions> Questions)
        {
            if (Questions == null)
            {
                Questions = await context.QuizQuestions
                        .Where(Q => Q.BibleId == BibleId 
                                && Q.BookNumber == BookNumber 
                                && Q.IsDeleted == false)
                        .ToListAsync();
            }
            InBookList = await IsInBooklistAsync(context);
            QuestionCount = GetQuestionCount(Questions);
            HasCommentary = await HasCommentaryAsync(context);
            if (HasCommentary)
            {
                CommentaryTitle = await GetCommentaryTitleAsync(context);
                CommentaryQuestionCount = GetCommentaryQuestionCount(Questions);
            }
            if (ChapterNum.HasValue)
            {   
                if (ChapterNum != Bibles.CommentaryChapter)
                {
                    BibleChapters Chapter = await context.BibleChapters
                                        .Where(C => C.BibleId == BibleId && C.BookNumber == BookNumber && C.ChapterNumber == ChapterNum)
                                        .SingleAsync();
                    Chapter.AddPBEChapterProperties(Questions);
                    this.BibleChapters.Add(Chapter);
                }
            }
            else
            {
                // Caller must have loaded Chapters
                foreach (BibleChapters Chapter in BibleChapters)
                {
                    Chapter.AddPBEChapterProperties(Questions);
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

        public int GetQuestionCount(List<QuizQuestions> Questions)
        {
            return Questions.Where(Q => Q.BookNumber == BookNumber 
                                    && Q.BibleId == BibleId 
                                    && Q.IsDeleted == false)
                            .Count();
        }

        public int GetCommentaryQuestionCount(List<QuizQuestions> Questions)
        {
            return Questions.Where(Q => Q.BookNumber == BookNumber 
                                    && Q.BibleId == BibleId 
                                    && Q.Chapter == Bibles.CommentaryChapter 
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

        public async Task<string> GetCommentaryTitleAsync(BiblePathsCoreDbContext context)
        {
            return (await context.CommentaryBooks
                             .Where(C => C.BibleId == BibleId && C.BookNumber == BookNumber)
                             .FirstAsync()).BookName;
        }

    }  
    public class MinBook
    {
        public string BibleId { get; set; }
        public string Testament { get; set; }
        public int? TestamentNumber { get; set; }
        public int BookNumber { get; set; }
        public string Name { get; set; }
        public int? Chapters { get; set; }

        public MinBook()
        {

        }

        public MinBook(BibleBooks Book)
        {
            BibleId = Book.BibleId;
            Testament = Book.Testament;
            TestamentNumber = Book.TestamentNumber;
            BookNumber = Book.BookNumber;
            Name = Book.Name;
            Chapters = Book.Chapters ?? 0; 
        }
    }
}
