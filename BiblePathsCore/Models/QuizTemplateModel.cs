using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class PredefinedQuiz
    {
        public const int MaxTemplateQuestions = 45;

        public List<PredefinedQuizQuestion> IntiQuestionListForAddEdit()
        {
            List<PredefinedQuizQuestion> ReturnQuestions = new List<PredefinedQuizQuestion>();
            
            // Step through the questions we already have adding filler questions where needed. 
            for (int i = 1; i <= NumQuestions; i++)
            {
                // Create an undefined or "Random" question and replace it with the real one if it exists.
                // We're stuffing the List with undefined or "Random" questions. 
                PredefinedQuizQuestion QuestionToAdd = new PredefinedQuizQuestion
                {
                    BookNumber = 0,
                    Chapter = 0,
                    QuestionNumber = i,
                };
                try
                {
                    QuestionToAdd = this.PredefinedQuizQuestions.Where(Q => Q.QuestionNumber == i).Single();
                }
                catch
                { 
                    // nothing to do here just catch and move on. 
                }
                ReturnQuestions.Add(QuestionToAdd);
            }
            return ReturnQuestions;
        }

        public async Task<List<MinBook>> GetTemplateBooksAsync(BiblePathsCoreDbContext context, string bibleId)
        {
            List<MinBook> ReturnList = new List<MinBook>();
            if (this.BookNumber < Bible.MinBookListID)
            {
                BibleBook Book = await context.BibleBooks.Where(B => B.BibleId == bibleId
                                                            && B.BookNumber == this.BookNumber)
                                                    .SingleAsync();
                Book.HasCommentary = await Book.HasCommentaryAsync(context);
                MinBook minBook = new MinBook(Book);
                ReturnList.Add(minBook);
            }
            else
            {
                QuizBookList BookList = await context.QuizBookLists.Where(L => L.Id == this.BookNumber
                                                                           && L.IsDeleted == false)
                                                                    .Include(L => L.QuizBookListBookMaps)
                                                                    .SingleAsync();

                foreach (QuizBookListBookMap bookMap in BookList.QuizBookListBookMaps)
                {
                    BibleBook BookMapBook = await context.BibleBooks.Where(B => B.BibleId == bibleId
                                            && B.BookNumber == bookMap.BookNumber)
                                    .SingleAsync();
                    BookMapBook.HasCommentary = await BookMapBook.HasCommentaryAsync(context);
                    MinBook minBook = new MinBook(BookMapBook);
                    ReturnList.Add(minBook);
                }                                                     

            }
            return ReturnList;
        }
        public static List<SelectListItem> GetCountSelectList()
        {
            List<SelectListItem> CountSelectList = new List<SelectListItem>();
            for (int i = 1; i <= MaxTemplateQuestions; i++)
            {
                CountSelectList.Add(new SelectListItem
                {
                    Text = i.ToString(),
                    Value = i.ToString(),
                });

            }
            return CountSelectList;
        }

        public static async Task<List<SelectListItem>> GetTemplateSelectListAsync(BiblePathsCoreDbContext context, QuizUser QuizUser)
        {

            List<SelectListItem> TemplateSelectList = new List<SelectListItem>();
            List<PredefinedQuiz> Templates = await context.PredefinedQuizzes
                                      .Where(T => T.QuizUser == QuizUser && T.IsDeleted == false)
                                      .ToListAsync();

            // Add a Default entry 
            TemplateSelectList.Add(new SelectListItem
            {
                Text = "<Select a Template>",
                Value = 0.ToString()
            });

            foreach (PredefinedQuiz Template in Templates)
            {
                TemplateSelectList.Add(new SelectListItem
                {
                    Text = Template.QuizName,
                    Value = Template.Id.ToString()
                });
            }
            return TemplateSelectList;
        }

        public static async Task<bool> CheckNameStaticAsync(BiblePathsCoreDbContext context, string CheckName)
        {
            var knownTerms = new[] { "create", "delete", "edit", "index", "mypaths", "path", "pathcomplete", "publish", "steps", "unpublish" };
            if (knownTerms.Contains(CheckName.ToLower()))
            {
                return true;
            }
            if (await context.Paths.Where(p => p.Name.ToLower() == CheckName.ToLower()).AnyAsync())
            {
                return true;
            }
            return false;
        }
    }

    public partial class PredefinedQuizQuestion
    {
        [NotMapped]
        public List<SelectListItem> ChapterSelectList { get; set; }

        public void AddChapterSelectList(List<MinBook> TemplateBooks)
        {
            List<SelectListItem> SelectList = new List<SelectListItem>();
            SelectList.Add(new SelectListItem
            {
                Text = "Random Chapter",
                Value = "0",
            });
            if (BookNumber > 0) // Bok Number of 0 just means no book selected
            {
                // Find our book
                MinBook SelectedBook = TemplateBooks.Where(B => B.BookNumber == BookNumber).Single();
                for (int i = 1; i <= SelectedBook.Chapters; i++)
                {
                    SelectList.Add(new SelectListItem
                    {
                        Text = i.ToString(),
                        Value = i.ToString(),
                    });

                }
                if (SelectedBook.HasCommentary == true)
                {
                    SelectList.Add(new SelectListItem
                    {
                        Text = "SDA Bible Commentary",
                        Value = "1000",
                    });
                }
            }
            ChapterSelectList = SelectList;
        }
    }
}
