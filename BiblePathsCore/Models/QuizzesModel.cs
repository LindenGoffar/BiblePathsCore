using BiblePathsCore.Pages.PBE;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyModel;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using static BiblePathsCore.Models.DB.QuizQuestion;

namespace BiblePathsCore.Models.DB
{
    public partial class QuizGroupStat
    {
        [NotMapped]
        public string BookOrTemplateName { get; set; }
        [NotMapped]
        public float Percentage { get; set; }
        [NotMapped]
        public int FITBQuestionCount { get; set; }
        [NotMapped]
        public List<QuizBookStats> BookStats { get; set; }
        [NotMapped]
        public int QuestionNumber { get; set; }

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
                PredefinedQuiz Template = await context.PredefinedQuizzes.FindAsync(PredefinedQuiz);
                if (Template != null){ BookOrTemplateName = Template.QuizName; }
                else { BookOrTemplateName = "Unknown";  }
            }
            else
            {
                BookOrTemplateName = await BibleBook.GetBookorBookListNameAsync(context, bibleId, BookNumber);
            }
            QuestionNumber = QuestionsAsked + 1;
            CalculateQuizStats();
            return retval;
        }

        public async Task<bool> AddDetailedQuizStatsAsync(BiblePathsCoreDbContext context, string bibleId)
        {
            bool retval = true;
            FITBQuestionCount = 0; 

            List<QuizBookStats> bookStats = new List<QuizBookStats>();
            // We need to retrieve all QuizQuestionStat Objects for this Quiz. 
            List<QuizQuestionStat> QuestionStats = await context.QuizQuestionStats.Where(S => S.QuizGroupId == Id &&
                                                                                         S.EventType == (int)QuizQuestion.QuestionEventType.QuestionPointsAwarded)
                                                                                    .ToListAsync();
            // Then we can iterate across each Question. 
            foreach (QuizQuestionStat Stat in QuestionStats)
            {
                QuizQuestion Question = await context.QuizQuestions.FindAsync(Stat.QuestionId);
                if (Question == null)
                {
                    // That's problematic and will mess up our stats but nothing we can do about it now. 
                }
                else
                {
                    QuizBookStats bookStat = new QuizBookStats();
                    try
                    {
                        bookStat = bookStats.Where(B => B.BookNumber == Question.BookNumber).Single();
                    }
                    catch
                    {
                        // Need to add a new bookStat to this quiz Object. 
                        bookStat.BookNumber = Question.BookNumber;
                        bookStat.BookName = await BibleBook.GetBookNameAsync(context, bibleId, Question.BookNumber);
                        bookStat.ChapterStats = new List<QuizChapterStats>();
                        bookStats.Add(bookStat);
                    }
                    // Now let's go update this bookStat
                    bookStat.AddQuestionToBookStat(Stat, Question);
                   
                    // Count our FITBQuestions
                    if (Question.Type == (int)QuestionType.FITB){
                        FITBQuestionCount++;
                    }
                }
            }
            BookStats = bookStats;
            return retval;
        }

        public async Task<bool> AddQuizPointsforQuestionAsync(BiblePathsCoreDbContext context, QuizQuestion QuestionToUpdate, int PointsToAward, QuizUser PBEUser)
        {
            bool retval = true;
            // Now we award the points... let's get this right: 
            // Let's prevent posting an anomalous number of points. 
            int QuestionPointsPossible = QuestionToUpdate.Points;
            if (PointsToAward > QuestionPointsPossible)
            { PointsToAward = QuestionPointsPossible; }
            if (PointsToAward < 0) { PointsToAward = 0; }

            // Update the Quiz Object: 
            context.Attach(this);
            PointsPossible += QuestionPointsPossible;
            PointsAwarded += PointsToAward;
            QuestionsAsked += 1;
            Modified = DateTime.Now;

            // Update the Question Object
            context.Attach(QuestionToUpdate);
            QuestionToUpdate.Type = QuestionToUpdate.DetectQuestionType();        
            QuestionToUpdate.LastAsked = DateTime.Now;

            // We've had some challenges with users challenging many questions often with no comment.
            // We will do a user check and make sure our user isn't blocked, if they are we silently fail the challenge.
            // TODO: We should revisit the silent fail if it becomes a problem. 
            // UPDATE: 12/13/2023 Quiz Challenge has been split from awarding points so this is 
            // deprecated code
            //if (Question.Challenged && !PBEUser.IsQuestionBuilderLocked)
            //{
            //    QuestionToUpdate.Challenged = true;
            //    QuestionToUpdate.ChallengeComment = Question.ChallengeComment;
            //    QuestionToUpdate.ChallengedBy = PBEUser.Email;
            //}

            // Save the changes to both the Quiz and Question objects. 
            await context.SaveChangesAsync();

            // And next let's make sure we log this event. 
            // BUG: Note we've had a pretty significant data bug prior to 6/8/2019 where we were setting Points to the cumulative quizGroupStat.PointsAwarded vs. the non-cumulative PointsAwardedByJudge... so all data prior to this date is wrong. 
            await QuestionToUpdate.RegisterEventAsync(context, QuestionEventType.QuestionPointsAwarded, PBEUser.Id, null, Id, PointsToAward);
            return retval;
        }

        public async Task<QuizQuestion> GetNextQuizQuestionAsync(BiblePathsCoreDbContext context, string bibleId)
        {
            QuizQuestion ReturnQuestion = new QuizQuestion
            {
                QuestionSelected = false
            };

            // Template Scenario
            if (PredefinedQuiz > 0)
            {
                ReturnQuestion = await GetNextQuizQuestionFromTemplateAsync(context, bibleId);
                return ReturnQuestion;
            }

            // BookList Scenario
            if (BookNumber >= Bible.MinBookListID)
            {
                ReturnQuestion = await GetNextQuizQuestionFromBookListAsync(context, bibleId, BookNumber);
                return ReturnQuestion;
            }
            
            // Book Scenario
            if (BookNumber > 0 && BookNumber < Bible.MinBookListID)
            {
                ReturnQuestion = await GetNextQuizQuestionFromBookAsync(context, bibleId, BookNumber);
                return ReturnQuestion;
            }

            return ReturnQuestion;
        }

        public async Task<QuizQuestion> GetNextQuizQuestionFromTemplateAsync(BiblePathsCoreDbContext context, string bibleId)
        {
            QuizQuestion ReturnQuestion = new QuizQuestion();
            PredefinedQuiz Template = new PredefinedQuiz();
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
            int TemplateQuestionNumber = QuestionNumber;
            // We need to calculate a Template Number 
            if (QuestionNumber > Template.NumQuestions) 
            { 
                TemplateQuestionNumber = QuestionNumber % Template.NumQuestions;
                if (TemplateQuestionNumber == 0) { TemplateQuestionNumber = Template.NumQuestions; }
            }
            // It is actually OK to not find a Question Object, we just treat that as the random book scenario.
            PredefinedQuizQuestion TemplateQuestion = new PredefinedQuizQuestion();
            try
            {
                TemplateQuestion = Template.PredefinedQuizQuestions.Where(Q => Q.QuestionNumber == TemplateQuestionNumber).First();
            }
            catch
            {
                // This is the more common pick a random Book Scenario.
                if (Template.BookNumber >= Bible.MinBookListID)
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
            if (TemplateQuestion.BookNumber == 0 ){                
                // This is the pick a random Book Scenario, we're unlikely to hit this, more often the question object won't exist at all.
                if (Template.BookNumber >= Bible.MinBookListID)
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

        public async Task<QuizQuestion> GetNextQuizQuestionFromBookListAsync(BiblePathsCoreDbContext context, string bibleId, int BookListId)
        {
            QuizQuestion ReturnQuestion = new QuizQuestion();
            QuizBookList BookList = new QuizBookList();
            int SelectedBookNumber = 0;
            try
            {
                BookList = await context.QuizBookLists.Include(L => L.QuizBookListBookMaps).Where(L => L.Id == BookListId).FirstAsync();
            }
            catch
            {
                // This is the couldn't find BookList scenario, 
                ReturnQuestion.QuestionSelected = false;
                return ReturnQuestion;
            }
            // Ok we've got our BookList Now... so which Book? 
            Random rand = new Random();
            if (BookList.QuizBookListBookMaps.Count > 0)
            {
                List<QuizBookListBookMap> Books = BookList.QuizBookListBookMaps.ToList();
                int BIndex = rand.Next(0, BookList.QuizBookListBookMaps.Count); // Rand will return an int >= 0 and < bookMaps.Count, which works with a zero based array right?
                SelectedBookNumber = Books[BIndex].BookNumber;
            }
            return await GetNextQuizQuestionFromBookAsync(context, bibleId, SelectedBookNumber);
        }

        public async Task<QuizQuestion> GetNextQuizQuestionFromBookAsync(BiblePathsCoreDbContext context, string bibleId, int BookNumber)
        {
            QuizQuestion ReturnQuestion = new QuizQuestion();
            BibleBook Book = new BibleBook();
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
        public async Task<QuizQuestion> GetNextQuizQuestionFromBookAndChapterAsync(BiblePathsCoreDbContext context, string bibleId, int BookNumber, int Chapter)
        {
            QuizQuestion ReturnQuestion = new QuizQuestion();
            List<QuizQuestion> PossibleQuestions = new List<QuizQuestion>();
            try
            {
                // We now query for 4 standard questions in the selected chapter
                // ordered by longest time since asked,
                // we want to avoid re-asking questions in a short period of time. 
                PossibleQuestions = await context.QuizQuestions.Where(Q => Q.BookNumber == BookNumber 
                                                                    && (Q.BibleId == bibleId || Q.BibleId == null)
                                                                    && Q.Chapter == Chapter
                                                                    && Q.Challenged == false 
                                                                    && Q.IsAnswered == true 
                                                                    && !(Q.IsDeleted)
                                                                    && Q.Type == (int)QuestionType.Standard)
                                                                .OrderBy(Q => Q.LastAsked).Take(4).ToListAsync();
            }
            catch
            {
                // We're not going to do anything here... we'll add some FITB
                // and move on. 
            }

            try
            {
                // We now query for 2 FITB questions in the selected chapter
                // ordered by longest time since asked,
                // we want to avoid re-asking questions in a short period of time. 
                PossibleQuestions.AddRange(await context.QuizQuestions.Where(Q => Q.BookNumber == BookNumber
                                                                    && (Q.BibleId == bibleId || Q.BibleId == null)
                                                                    && Q.Chapter == Chapter
                                                                    && Q.Challenged == false
                                                                    && Q.IsAnswered == true
                                                                    && !(Q.IsDeleted)
                                                                    && Q.Type == (int)QuestionType.FITB)
                                                                .OrderBy(Q => Q.LastAsked).Take(2).ToListAsync()
                                            );
            }
            catch
            {
                // We're not going to do anything here... 
                // We'll catch the no questions scenario below.
            }


            if (PossibleQuestions == null)
            {
                // This is the couldn't find a question scenario.
                ReturnQuestion.QuestionSelected = false;
                return ReturnQuestion;
            }

            if (PossibleQuestions.Count == 0)
            {
                // This is another couldn't find a question scenario.
                ReturnQuestion.QuestionSelected = false;
                return ReturnQuestion;
            }

            if (PossibleQuestions.Count > 0)
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

        public void AddQuestionToBookStat(QuizQuestionStat stat, QuizQuestion question)
        {
            QuizChapterStats chapterStat = new QuizChapterStats();
            try
            {
                chapterStat = ChapterStats.Where(C => C.Chapter == question.Chapter).Single();
            }
            catch
            {
                // Need to add a new chapter stat object.
                chapterStat.Chapter = question.Chapter;
                ChapterStats.Add(chapterStat);
            }
            // Now let's go update this chapterStat
            chapterStat.AddQuestionToChapterStat(stat, question);

            // and Finally update thisBookStat.
            QuestionsAsked++;
            if (stat.Points.HasValue) { PointsAwarded += stat.Points.Value; }
            PointsPossible += question.Points;
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

        public void AddQuestionToChapterStat(QuizQuestionStat stat, QuizQuestion question)
        {
            QuestionsAsked++;
            if (stat.Points.HasValue) { PointsAwarded += stat.Points.Value; }
            PointsPossible += question.Points;
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
    }

}
