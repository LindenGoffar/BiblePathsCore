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
    public partial class QuizGroupStats
    {
        [NotMapped]
        public string BookOrTemplateName { get; set; }
        [NotMapped]
        public float Percentage { get; set; }
        [NotMapped]
        public List<QuizBookStats> BookStats { get; set; }

        public void CalculateQuizStats()
        {
            if (PointsPossible > 0)
            {
                Percentage = ((float)PointsAwarded / PointsPossible) * 100;
                Percentage = (float)Math.Round((Decimal)Percentage, 2);
            }
            else
            {
                Percentage = 0;
            }
        }

        public async Task<bool> AddQuizPropertiesAsync(BiblePathsCoreDbContext context, string bibleId)
        {
            bool retval = true;
            // If we're using a Template we'll use that name, otherwise well use 
            // a Book or Booklist name. 
            if(PredefinedQuiz > 0) // Template Scenario
            {
                PredefinedQuizzes Template = await context.PredefinedQuizzes.FindAsync(PredefinedQuiz);
                if (Template != null){ BookOrTemplateName = Template.QuizName; }
                else { BookOrTemplateName = "Unknown";  }
            }
            else
            {
                BookOrTemplateName = await BibleBooks.GetBookorBookListNameAsync(context, bibleId, BookNumber);
            }
            CalculateQuizStats();
            return retval;
        }

        public async Task<QuizQuestions> GetNextQuizQuestionAsync(BiblePathsCoreDbContext context, string bibleId)
        {
            QuizQuestions ReturnQuestion = new QuizQuestions();
            ReturnQuestion.QuestionSelected = false;

            // Template Scenario
            if (PredefinedQuiz > 0)
            {
                ReturnQuestion = await GetNextQuizQuestionFromTemplateAsync(context, bibleId);
            }

            // BookList Scenario
            if (BookNumber >= Bibles.MinBookListID)
            {
                ReturnQuestion = await GetNextQuizQuestionFromBookListAsync(context, bibleId, BookNumber);
            }
            
            // Book Scenario
            if (BookNumber > 0)
            {
                ReturnQuestion = await GetNextQuizQuestionFromBookAsync(context, bibleId, BookNumber);
            }

            return ReturnQuestion;
        }

        public async Task<QuizQuestions> GetNextQuizQuestionFromTemplateAsync(BiblePathsCoreDbContext context, string bibleId)
        {
            QuizQuestions ReturnQuestion = new QuizQuestions();
            PredefinedQuizzes Template = new PredefinedQuizzes();
            try
            {
                Template = await context.PredefinedQuizzes.Include(T => T.PredefinedQuizQuestions).Where(T => T.Id == PredefinedQuiz).FirstAsync();
            }
            catch
            {
                // This is the couldn't find template scenario, 
                ReturnQuestion.QuestionSelected = false;
                return ReturnQuestion; 
            }
            // Ok we've got our Template now which Book/Chapter do we want. 
            int QuestionNumber = QuestionsAsked + 1;
            PredefinedQuizQuestions TemplateQuestion = Template.PredefinedQuizQuestions.Where(Q => Q.QuestionNumber == QuestionNumber).First();
            if (TemplateQuestion.BookNumber == 0 ){                
                // This is the pick a random Book Scenario
                if (Template.BookNumber >= Bibles.MinBookListID)
                {
                    // This is the BookList Scenario
                    return await GetNextQuizQuestionFromBookListAsync(context, bibleId, Template.BookNumber);
                }
                else
                {
                    // this is the Book scenario.
                    return await GetNextQuizQuestionFromBookAsync(context, bibleId, Template.BookNumber);
                }
            }
            else
            {
                // This would be the selected Book Scenario, but do we have a selected Chapter? 
                if (TemplateQuestion.Chapter == 0)
                {
                    // this is the selected book but random Chapter scenario
                    return await GetNextQuizQuestionFromBookAsync(context, bibleId, TemplateQuestion.BookNumber);
                }
                else
                {
                    // this is the selected Book and Chapter scenario
                    return await GetNextQuizQuestionFromBookAndChapterAsync(context, bibleId, TemplateQuestion.BookNumber, TemplateQuestion.Chapter);
                }

            }
        }

        public async Task<QuizQuestions> GetNextQuizQuestionFromBookListAsync(BiblePathsCoreDbContext context, string bibleId, int BookListId)
        {
            QuizQuestions ReturnQuestion = new QuizQuestions();
            QuizBookLists BookList = new QuizBookLists();
            int SelectedBookNumber = 0;
            try
            {
                BookList = await context.QuizBookLists.Include(L => L.QuizBookListBookMap).Where(L => L.Id == BookListId).FirstAsync();
            }
            catch
            {
                // This is the couldn't find BookList scenario, 
                ReturnQuestion.QuestionSelected = false;
                return ReturnQuestion;
            }
            // Ok we've got our BookList Now... so which Book? 
            Random rand = new Random();
            if (BookList.QuizBookListBookMap.Count > 0)
            {
                List<QuizBookListBookMap> Books = BookList.QuizBookListBookMap.ToList();
                int BIndex = rand.Next(0, BookList.QuizBookListBookMap.Count); // Rand will return an int >= 0 and < bookMaps.Count, which works with a zero based array right?
                SelectedBookNumber = Books[BIndex].BookNumber;
            }
            return await GetNextQuizQuestionFromBookAsync(context, bibleId, SelectedBookNumber);
        }

        public async Task<QuizQuestions> GetNextQuizQuestionFromBookAsync(BiblePathsCoreDbContext context, string bibleId, int BookNumber)
        {
            QuizQuestions ReturnQuestion = new QuizQuestions();
            BibleBooks Book = new BibleBooks();
            int SelectedChapter = 0; 
            try
            {
                Book = await context.BibleBooks.Where(B => B.BibleId == bibleId && B.BookNumber == BookNumber).FirstAsync();
            }
            catch
            {
                // This is the couldn't find the book scenario.
                ReturnQuestion.QuestionSelected = false;
                return ReturnQuestion;
            }

            // Ok we've got our Book... now which chapter? pick a random one
            Random rand = new Random();
            if (Book.Chapters.HasValue == true)
            {
                SelectedChapter = rand.Next(Book.Chapters.Value) + 1; // Rand will return an int >= 0 and < Book.Chapters, so we add 1 since Chapters are 1 based. 
            }
            else
            {
                // Somethings badly wrong with this Book it has not chapters
                ReturnQuestion.QuestionSelected = false;
                return ReturnQuestion;
            }

            return await GetNextQuizQuestionFromBookAndChapterAsync(context, bibleId, BookNumber, SelectedChapter);
        }
        public async Task<QuizQuestions> GetNextQuizQuestionFromBookAndChapterAsync(BiblePathsCoreDbContext context, string bibleId, int BookNumber, int Chapter)
        {
            QuizQuestions ReturnQuestion = new QuizQuestions();
            List<QuizQuestions> PossibleQuestions = new List<QuizQuestions>();
            try
            {
                // We now query for 5 questions in the selected chapter ordered by longest time since asked, we want to avoid re-asking questions in a short period of time. 
                PossibleQuestions = await context.QuizQuestions.Where(Q => Q.BookNumber == BookNumber && Q.Challenged == false && Q.IsAnswered == true && !(Q.IsDeleted)).OrderBy(Q => Q.LastAsked).Take(5).ToListAsync();
            }
            catch
            {
                // This is the couldn't find the book scenario.
                ReturnQuestion.QuestionSelected = false;
                return ReturnQuestion;
            }

            if(PossibleQuestions.Count > 0)
            {
                Random rand = new Random();
                int QIndex = rand.Next(0, PossibleQuestions.Count); // Rand will return an int >= 0 and < PossibleQuestions.Count, which works with a zero based array right?
                ReturnQuestion = PossibleQuestions[QIndex];
                if (ReturnQuestion.IsAnswered)
                { 
                    // Load our possible answers. 
                    context.Entry(ReturnQuestion)
                        .Collection(Q => Q.QuizAnswers)
                        .Load();
                    ReturnQuestion.QuestionSelected = true;
                    return ReturnQuestion;
                }
            }
            ReturnQuestion.QuestionSelected = false;
            return ReturnQuestion;
        }
    }


    public class QuizBookStats
    {
        public int BookNumber { get; set; }
        public string BookName { get; set; }
        public int QuestionsAsked { get; set; }
        public int PointsPossible { get; set; }
        public int PointsAwarded { get; set; }
        public float Percentage { get; set; }
        public List<QuizChapterStats> ChapterStats { get; set; }
        public QuizBookStats()
        {
            BookNumber = 0;
            BookName = "";
            QuestionsAsked = 0;
            PointsPossible = 0;
            PointsAwarded = 0;
            Percentage = 0;
        }
    }

    public class QuizChapterStats
    {
        public int Chapter { get; set; }
        public int QuestionsAsked { get; set; }
        public int PointsPossible { get; set; }
        public int PointsAwarded { get; set; }
        public float Percentage { get; set; }
        public QuizChapterStats()
        {
            Chapter = 0;
            QuestionsAsked = 0;
            PointsPossible = 0;
            PointsAwarded = 0;
            Percentage = 0;
        }
    }

}
