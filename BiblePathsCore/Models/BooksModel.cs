using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class BibleBook
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
        [NotMapped]
        public bool HasChallenge { get; set; }
        [NotMapped]
        public bool CommentaryHasChallenge { get; set; }

        public static async Task<string> GetBookNameAsync(BiblePathsCoreDbContext context, string bibleId, int BookNumber)
        {
            // Get BookName 
            return (await context.BibleBooks.Where(B => B.BibleId == bibleId && B.BookNumber == BookNumber).Select(B => new { B.Name }).FirstAsync()).Name;
        }

        public static async Task<string> GetBookorBookListNameAsync(BiblePathsCoreDbContext context, string bibleId, int BookNumber)
        {
            // BookList scenario
            if(BookNumber >= Bible.MinBookListID)
            {
                return (await context.QuizBookLists.Where(L => L.Id == BookNumber).Select(L => new { L.BookListName }).FirstAsync()).BookListName;
            }
            else
            {
                // Get BookName 
                return await BibleBook.GetBookNameAsync(context, bibleId, BookNumber);
            }
        }

        public static async Task<BibleBook> GetBookByNameAsync(BiblePathsCoreDbContext context, string BibleId, string BookName)
        {
            BibleBook Book = new BibleBook();
            try
            {
                Book = await context.BibleBooks.Include(B => B.BibleChapters)
                                                .Where(B => B.BibleId == BibleId
                                                        && B.Name == BookName)
                                                .SingleAsync();
            }
            catch
            {
                return null;
            }
            return Book;
        }
        public static async Task<BibleBook> GetBookAndChapterByNameAsync(BiblePathsCoreDbContext context, string BibleId, string BookName, int ChapterNum)
        {
            BibleBook PBEBook = new BibleBook();
            try
            {
                PBEBook = await context.BibleBooks.Where(B => B.BibleId == BibleId 
                                                        && B.Name == BookName)
                                                  .SingleAsync();
            }
            catch
            {
                return null; 
            }
            
            // TODO: This is not ideal, we should be simply be deleting rather than soft deleting these
            //       So that a simple ANY would work vs. having to retrieve all of these.  
            //List<QuizBookList> BookLists = await context.QuizBookLists
            //                                    .Include(L => L.QuizBookListBookMaps)
            //                                    .Where(L => L.IsDeleted == false)
            //                                    .ToListAsync();

            await PBEBook.AddPBEBookPropertiesAsync(context, ChapterNum, null);
            return PBEBook;
        }

        public static async Task<BibleBook> GetPBEBookAndChapterAsync(BiblePathsCoreDbContext context, string BibleId, int BookNumber, int ChapterNum)
        {
            BibleBook PBEBook = await context.BibleBooks
                                                  .Where(B => B.BibleId == BibleId && B.BookNumber == BookNumber)
                                                  .SingleAsync();
            if (PBEBook == null) { return null; }

            // TODO: This is not ideal, we should be simply be deleting rather than soft deleting these
            //       So that a simple ANY would work vs. having to retrieve all of these.
            // TODO: how important really is the inBookList check? 
            //List<QuizBookList> BookLists = await context.QuizBookLists
            //                                    .Include(L => L.QuizBookListBookMaps)
            //                                    .Where(L => L.IsDeleted == false)
            //                                    .ToListAsync();

            await PBEBook.AddPBEBookPropertiesAsync(context, ChapterNum, null);
            return PBEBook;
        }
        public static async Task<IList<BibleBook>> GetPBEBooksAsync(BiblePathsCoreDbContext context, string BibleId, bool recent = true)
        {
            IList<BibleBook> PBEBooks = await context.BibleBooks
                                                  .Include(B => B.BibleChapters)
                                                  .Where(B => B.BibleId == BibleId)
                                                  .ToListAsync();
            // Querying for Question counts for each Book/Chapter gets expensive let's grab all of them
            // and pass them around for counting.
            // Switch to Static Method
            List<QuizQuestion> Questions = await QuizQuestion.GetQuestionOnlyListAsync(context, BibleId, recent);

            // TODO: This is not ideal, we should be simply be deleting rather than soft deleting these
            //       So that a simple ANY would work vs. having to retrieve all of these.  
            //List <QuizBookList> BookLists = await context.QuizBookLists
            //                                    .Include(L => L.QuizBookListBookMaps)
            //                                    .Where(L => L.IsDeleted == false)
            //                                    .ToListAsync();

            foreach (BibleBook Book in PBEBooks)
            {
                await Book.AddPBEBookPropertiesAsync(context, null, Questions);
            }
            return PBEBooks;
        }

        public static async Task<List<SelectListItem>> GetBookSelectListAsync(BiblePathsCoreDbContext context, string BibleId)
        {

            List<SelectListItem> BookSelectList = new List<SelectListItem>();
            List<BibleBook> Books = await context.BibleBooks
                                      .Where(B => B.BibleId == BibleId)
                                      .ToListAsync();

            // Add a Default entry 
            BookSelectList.Add(new SelectListItem
            {
                Text = "Select a Book",
                Value = 0.ToString()
            });

            foreach (BibleBook Book in Books)
            {
                BookSelectList.Add(new SelectListItem
                {
                    Text = Book.Name,
                    Value = Book.BookNumber.ToString()
                }) ;
            }
            return BookSelectList;
        }

        public static async Task<List<SelectListItem>> GetCommentaryBookSelectListAsync(BiblePathsCoreDbContext context, string BibleId, int BookNum = 0, bool IncludeBooksWithCommentary = false)
        {

            List<SelectListItem> BookSelectList = new List<SelectListItem>();
            List<BibleBook> Books = await context.BibleBooks
                                      .Where(B => B.BibleId == BibleId)
                                      .ToListAsync();

            // Removing a bunch of older logic that restricted to a single Commentary per Book.
            //List<CommentaryBook> commentaryBooks = await context.CommentaryBooks
            //                                         .Where(C => C.BibleId == BibleId)
            //                                         .ToListAsync();

            // Add a Default entry 
            BookSelectList.Add(new SelectListItem
            {
                Text = "Select a Book",
                Value = 0.ToString()
            });

            foreach (BibleBook Book in Books)
            {
                // If there is already a Commentary entry for this book we skip it...
                // unless IncludeBooksWithCommentary is true
                // or unless this is the Book for the commentary we are editing the Booknum > 0 case
                // bool CommentaryExists = commentaryBooks.Where(C => C.BibleId == BibleId && C.BookNumber == Book.BookNumber).Any();
                //if (!CommentaryExists || IncludeBooksWithCommentary || Book.BookNumber == BookNum)
                BookSelectList.Add(new SelectListItem
                {
                    Text = Book.Name,
                    Value = Book.BookNumber.ToString()
                });
            }
            return BookSelectList;
        }

        public static async Task<List<SelectListItem>> GetBookAndBookListSelectListAsync(BiblePathsCoreDbContext context, string BibleId)
        {

            List<SelectListItem> BookSelectList = new List<SelectListItem>();

            List<QuizBookList> BookLists = await context.QuizBookLists
                                                .Where(L => L.IsDeleted == false)
                                                .OrderByDescending(L => L.Created)
                                                .ToListAsync();

            List<BibleBook> Books = await context.BibleBooks
                                      .Where(B => B.BibleId == BibleId)
                                      .OrderBy(B => B.BookNumber)
                                      .ToListAsync();

            // Add a Default entry 
            BookSelectList.Add(new SelectListItem
            {
                Text = "Select a Book or BookList",
                Value = 0.ToString()
            });

            // Add our BookLists first
            foreach (QuizBookList BookList in BookLists)
            {
                BookSelectList.Add(new SelectListItem
                {
                    Text = BookList.BookListName,
                    Value = BookList.Id.ToString()
                });
            }

            foreach (BibleBook Book in Books)
            {
                BookSelectList.Add(new SelectListItem
                {
                    Text = Book.Name,
                    Value = Book.BookNumber.ToString()
                });
            }
            return BookSelectList;
        }
        public static async Task<List<BibleBook>> GetPBEBooksWithQuestionsAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            List<BibleBook> ReturnBooks = new List<BibleBook>();
            List<BibleBook> PBEBooks = await context.BibleBooks
                                                  .Where(B => B.BibleId == BibleId)
                                                  .ToListAsync();
            // Querying for Question counts for each Book/Chapter gets expensive let's grab all of them
            // and pass them around for counting.
            // Switch to Static Method
            List<QuizQuestion> Questions = await QuizQuestion.GetQuestionOnlyListAsync(context, BibleId);
            //List<QuizQuestion> Questions = await context.QuizQuestions
            //                                            .Where(Q => (Q.BibleId == BibleId || Q.BibleId == null)
            //                                                    && Q.IsDeleted == false
            //                                                    && Q.Type == (int)QuestionType.Standard)
            //                                            .ToListAsync();

            foreach (BibleBook Book in PBEBooks)
            {
                Book.QuestionCount = Book.GetQuestionCount(Questions);
                if (Book.QuestionCount > 0)
                {
                    ReturnBooks.Add(Book);
                }
            }
            return ReturnBooks;
        }

        public async Task<bool> AddPBEBookPropertiesAsync(BiblePathsCoreDbContext context, int? ChapterNum, List<QuizQuestion> Questions)
        {
            if (Questions == null)
            {
                Questions = await QuizQuestion.GetQuestionOnlyListAsync(context, BibleId, BookNumber);
            }

            //TODO: Find out where and how often this is used... Seems super wasteful.
            InBookList = await IsInBookListAsync(context);
            //InBookList = IsInBooklist(context, BookLists);

            QuestionCount = GetQuestionCount(Questions);
            HasChallenge = HasChallengedQuestion(Questions);

            // This check uses an Any will work fine with the multi Commentary books scenario 
            HasCommentary = await HasCommentaryAsync(context);
            if (HasCommentary)
            {
                CommentaryTitle = await GetFullCommentaryTitleAsync(context); // Uses a FirstAsync will work with multi-commentary books.
                CommentaryQuestionCount = GetCommentaryQuestionCount(Questions);
                CommentaryHasChallenge = CommentaryHasChallengedQuestion(Questions);
            }
            if (ChapterNum.HasValue)
            {   
                if (ChapterNum != Bible.CommentaryChapter)
                {
                    BibleChapter Chapter = await context.BibleChapters
                                        .Where(C => C.BibleId == BibleId && C.BookNumber == BookNumber && C.ChapterNumber == ChapterNum)
                                        .SingleAsync();
                    Chapter.AddPBEChapterProperties(Questions);
                    this.BibleChapters.Add(Chapter);
                }
                else // What if it is a Commentary? 
                {
                    // in the commentary scenario there is no Chapter in the DB so no actions make good sense here. 
                }
                
            }
            else
            {
                // Caller must have loaded Chapters
                foreach (BibleChapter Chapter in BibleChapters)
                {
                    Chapter.AddPBEChapterProperties(Questions);
                }
            }
            return true;
        }

        public bool IsInBooklist(BiblePathsCoreDbContext context, List<QuizBookList> BookLists)
        {
            // We need to determine whether any of the non-deleted BookLists contains our Book.
            foreach (QuizBookList list in BookLists)
            {
                if (list.QuizBookListBookMaps.Where(B => B.BookNumber == BookNumber).Any())
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> IsInBookListAsync(BiblePathsCoreDbContext context)
        {
            return await context.QuizBookListBookMaps.
                Where(B => B.BookNumber == BookNumber).AnyAsync();
        }

        public async Task<bool> HasCommentaryAsync(BiblePathsCoreDbContext context)
        {
            return await context.CommentaryBooks
                             .Where(C => C.BibleId == BibleId && C.BookNumber == BookNumber)
                             .AnyAsync();
        }

        public int GetQuestionCount(List<QuizQuestion> Questions)
        {
            return Questions.Where(Q => Q.BookNumber == BookNumber 
                                    && (Q.BibleId == BibleId || Q.BibleId == null)
                                    && Q.IsDeleted == false)
                            .Count();
        }

        public bool HasChallengedQuestion(List<QuizQuestion> Questions)
        {
            return Questions.Where(Q => Q.BookNumber == BookNumber
                                    && (Q.BibleId == BibleId || Q.BibleId == null)
                                    && Q.IsDeleted == false
                                    && Q.Challenged == true)
                            .Any();
        }

        public int GetCommentaryQuestionCount(List<QuizQuestion> Questions)
        {
            return Questions.Where(Q => Q.BookNumber == BookNumber
                                    && (Q.BibleId == BibleId || Q.BibleId == null)
                                    && Q.Chapter == Bible.CommentaryChapter
                                    && Q.IsDeleted == false)
                        .Count();
        }
        public bool CommentaryHasChallengedQuestion(List<QuizQuestion> Questions)
        {
            return Questions.Where(Q => Q.BookNumber == BookNumber
                                    && (Q.BibleId == BibleId || Q.BibleId == null)
                                    && Q.Chapter == Bible.CommentaryChapter
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

        public async Task<string> GetFullCommentaryTitleAsync(BiblePathsCoreDbContext context)
        {
            CommentaryBook Commentary = await context.CommentaryBooks
                             .Where(C => C.BibleId == BibleId && C.BookNumber == BookNumber)
                             .FirstAsync();
            string returnString = Commentary.CommentaryTitle + " for " + Commentary.BookName;

            return returnString;
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
        public bool HasCommentary { get; set; }

        public MinBook()
        {

        }
        public MinBook(BibleBook Book)
        {
            BibleId = Book.BibleId;
            Testament = Book.Testament;
            TestamentNumber = Book.TestamentNumber;
            BookNumber = Book.BookNumber;
            Name = Book.Name;
            Chapters = Book.Chapters ?? 0;
            HasCommentary = Book.HasCommentary;
        }

        public static List<SelectListItem> GetMinBookSelectListFromList(List<MinBook> MinBooks)
        {
            List<SelectListItem> BookSelectList = new List<SelectListItem>();
            // Add a Default entry for Template Book Selection
            BookSelectList.Add(new SelectListItem
            {
                Text = "Random Book",
                Value = "0"
            });

            foreach (MinBook Book in MinBooks)
            {
                BookSelectList.Add(new SelectListItem
                {
                    Text = Book.Name,
                    Value = Book.BookNumber.ToString()
                });
            }
            return BookSelectList;
        }
    }
}
