using Microsoft.AspNetCore.Mvc.Rendering;
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

        public static async Task<string> GetBookNameAsync(BiblePathsCoreDbContext context, string bibleId, int BookNumber)
        {
            // Get BookName 
            return (await context.BibleBooks.Where(B => B.BibleId == bibleId && B.BookNumber == BookNumber).Select(B => new { B.Name }).FirstAsync()).Name;
        }
        public static async Task<BibleBooks> GetBookAndChapterByNameAsync(BiblePathsCoreDbContext context, string BibleId, string BookName, int ChapterNum)
        {
            BibleBooks PBEBook = await context.BibleBooks
                                                  .Where(B => B.BibleId == BibleId && B.Name.ToLower().Contains(BookName.ToLower()))
                                                  .SingleAsync();
            if (PBEBook == null) { return null; }

            // TODO: This is not ideal, we should be simply be deleting rather than soft deleting these
            //       So that a simple ANY would work vs. having to retrieve all of these.  
            List<QuizBookLists> BookLists = await context.QuizBookLists
                                                .Include(L => L.QuizBookListBookMap)
                                                .Where(L => L.IsDeleted == false)
                                                .ToListAsync();

            await PBEBook.AddPBEBookPropertiesAsync(context, ChapterNum, null, BookLists);
            return PBEBook;
        }

        public static async Task<BibleBooks> GetPBEBookAndChapterAsync(BiblePathsCoreDbContext context, string BibleId, int BookNumber, int ChapterNum)
        {
            BibleBooks PBEBook = await context.BibleBooks
                                                  .Where(B => B.BibleId == BibleId && B.BookNumber == BookNumber)
                                                  .SingleAsync();
            if (PBEBook == null) { return null; }

            // TODO: This is not ideal, we should be simply be deleting rather than soft deleting these
            //       So that a simple ANY would work vs. having to retrieve all of these.  
            List<QuizBookLists> BookLists = await context.QuizBookLists
                                                .Include(L => L.QuizBookListBookMap)
                                                .Where(L => L.IsDeleted == false)
                                                .ToListAsync();

            await PBEBook.AddPBEBookPropertiesAsync(context, ChapterNum, null, BookLists);
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

            // TODO: This is not ideal, we should be simply be deleting rather than soft deleting these
            //       So that a simple ANY would work vs. having to retrieve all of these.  
            List<QuizBookLists> BookLists = await context.QuizBookLists
                                                .Include(L => L.QuizBookListBookMap)
                                                .Where(L => L.IsDeleted == false)
                                                .ToListAsync();

            foreach (BibleBooks Book in PBEBooks)
            {
                await Book.AddPBEBookPropertiesAsync(context, null, Questions, BookLists);
            }
            return PBEBooks;
        }

        public static async Task<List<SelectListItem>> GetBookSelectListAsync(BiblePathsCoreDbContext context, string BibleId)
        {

            List<SelectListItem> BookSelectList = new List<SelectListItem>();
            List<BibleBooks> Books = await context.BibleBooks
                                      .Where(B => B.BibleId == BibleId)
                                      .ToListAsync();

            // Add a Default entry 
            BookSelectList.Add(new SelectListItem
            {
                Text = " ",
                Value = 0.ToString()
            });

            foreach (BibleBooks Book in Books)
            {
                BookSelectList.Add(new SelectListItem
                {
                    Text = Book.Name,
                    Value = Book.BookNumber.ToString()
                }) ;
            }
            return BookSelectList;
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

        public async Task<bool> AddPBEBookPropertiesAsync(BiblePathsCoreDbContext context, int? ChapterNum, List<QuizQuestions> Questions, List<QuizBookLists> BookLists)
        {
            if (Questions == null)
            {
                Questions = await context.QuizQuestions
                        .Where(Q => Q.BibleId == BibleId 
                                && Q.BookNumber == BookNumber 
                                && Q.IsDeleted == false)
                        .ToListAsync();
            }
            InBookList = IsInBooklist(context, BookLists);
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

        public bool IsInBooklist(BiblePathsCoreDbContext context, List<QuizBookLists> BookLists)
        {
            // We need to determine whether any of the non-deleted BookLists contains our Book.
            foreach (QuizBookLists list in BookLists)
            {
                if (list.QuizBookListBookMap.Where(B => B.BookNumber == BookNumber).Any())
                {
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
