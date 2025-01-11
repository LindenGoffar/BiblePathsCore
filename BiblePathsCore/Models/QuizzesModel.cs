using BiblePathsCore.Pages.PBE;
using BiblePathsCore.Services;
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
using System.Reflection.Metadata.Ecma335;
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
        [NotMapped]
        public List<QuestionHistory> QuizHistory { get; set; }

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
            if (PredefinedQuiz > 0) // Template Scenario
            {
                PredefinedQuiz Template = await context.PredefinedQuizzes.FindAsync(PredefinedQuiz);
                if (Template != null) { BookOrTemplateName = Template.QuizName; }
                else { BookOrTemplateName = "Unknown"; }
            }
            else
            {
                BookOrTemplateName = await BibleBook.GetBookorBookListNameAsync(context, bibleId, BookNumber);
            }
            QuestionNumber = QuestionsAsked + 1;
            CalculateQuizStats();
            return retval;
        }

        public async Task<bool> AddMockQuizPropertiesAsync(BiblePathsCoreDbContext context, string bibleId)
        {
            bool retval = true;
            // If we're using a Template we'll use that name, otherwise well use 
            // a Book or Booklist name. 
            if (PredefinedQuiz > 0) // Template Scenario
            {
                PredefinedQuiz Template = await context.PredefinedQuizzes.FindAsync(PredefinedQuiz);
                if (Template != null) { BookOrTemplateName = Template.QuizName; }
                else { BookOrTemplateName = "Unknown"; }
            }
            else
            {
                BookOrTemplateName = await BibleBook.GetBookorBookListNameAsync(context, bibleId, BookNumber);
            }
            QuestionNumber = QuestionsAsked + 1;
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

                    // Special case for Commentary we need that Section Title. 
                    if (Question.Chapter == Bible.CommentaryChapter)
                    {
                        Question.BookName = bookStat.BookName; // set this cause we need it for the Commentary scenario
                        Question.Verses = await Question.GetCommentaryMetadataAsVersesAsync(context, true);
                    }
                    // Now let's go update this bookStat
                    bookStat.AddQuestionToBookStat(Stat, Question);

                    // Count our FITBQuestions
                    if (Question.Type == (int)QuestionType.FITB)
                    {
                        FITBQuestionCount++;
                    }
                }
            }
            BookStats = bookStats;
            return retval;
        }

        public async Task<bool> AddQuizHistoryAsync(BiblePathsCoreDbContext context, string bibleId)
        {
            bool retval = true;
            
            List<QuestionHistory> quizHistory = new List<QuestionHistory>();    

            // We need to retrieve all QuizQuestionStat Objects for this Quiz to date.
            List<QuizQuestionStat> QuestionStats = await context.QuizQuestionStats.Where(S => S.QuizGroupId == Id &&
                                                                                         S.EventType == (int)QuizQuestion.QuestionEventType.QuestionPointsAwarded)
                                                                                    .OrderBy(S => S.EventWritten)
                                                                                    .ToListAsync();
            // Then we can iterate across each Question adding it to history.
            int QNumber = 1;
            foreach (QuizQuestionStat Stat in QuestionStats)
            {
                QuizQuestion Question = await context.QuizQuestions.FindAsync(Stat.QuestionId);
                if (Question == null)
                {
                    // That's problematic and will mess up our history but nothing we can do about it now. 
                }
                else
                {
                    QuestionHistory QuestionAsked = new QuestionHistory();

                    //We can't simply add a question, first we must populate some info on the question.
                    BibleBook PBEBook = await BibleBook.GetPBEBookAndChapterAsync(context, bibleId, Question.BookNumber, Question.Chapter);
                    if (Question.Chapter == Bible.CommentaryChapter)
                    {
                        Question.Verses = await Question.GetCommentaryMetadataAsVersesAsync(context, true);
                    }
                    Question.PopulatePBEQuestionInfo(PBEBook);


                    QuestionAsked.AddQuestionToQuestionHistory(Stat, Question);
                    QuestionAsked.QuestionNumber = QNumber;
                    quizHistory.Add(QuestionAsked);
                    QNumber++;
                }
            }
            QuizHistory = quizHistory;
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
            // Note: This will flip any AIProposed questions to Standard 
            QuestionToUpdate.Type = QuestionToUpdate.DetectQuestionType();
            QuestionToUpdate.LastAsked = DateTime.Now;

            // Save the changes to both the Quiz and Question objects. 
            await context.SaveChangesAsync();

            // And next let's make sure we log this event. 
            // BUG: Note we've had a pretty significant data bug prior to 6/8/2019 where we were setting Points to the cumulative quizGroupStat.PointsAwarded vs. the non-cumulative PointsAwardedByJudge... so all data prior to this date is wrong. 
            await QuestionToUpdate.RegisterEventAsync(context, QuestionEventType.QuestionPointsAwarded, PBEUser.Id, null, Id, PointsToAward);
            return retval;
        }

        public async Task<QuizQuestion> GetOrBuildNextQuizQuestionAsync(BiblePathsCoreDbContext context, string bibleId, IOpenAIResponder openAIResponder, QuizUser pbeUser)
        {

            QuizQuestion question = new QuizQuestion();

            // We're going to take 3 swings at this for the scenario where we don't have enough questions. 
            int iterations = 0;
            do
            {
                question = await GetNextQuizQuestionAsync(context, bibleId);
                iterations++;
            }
            while (iterations < 3 && question.QuestionSelected == false);

            // This is the no questions found scenario, let's try an AI Generated Question.
            // the previous attempts at finding a question should have supplied the Book and Chapter on the question Object. 
            if (question.QuestionSelected == false)
            {
                // We need to select a random Verse to build a temporary question for. 
                // this should account correctly for Excluded verses by not returning them. 
                BibleVerse bibleVerse = await question.GetRandomVerseAsync(context);

                QuizQuestion BuiltQuestion = new();
                BuiltQuestion = await question.BuildAIQuestionForVerseAsync(context, bibleVerse, openAIResponder);
                if (BuiltQuestion != null)
                {
                    question = BuiltQuestion;
                    question.Type = (int)QuestionType.AIProposed; // this is a temporary type so we can make appropriate decisions.
                    question.Challenged = true; // We start these out challenged, because we don't fully trust them, if points are assigned by the user then we remove the challenge. 
                    question.ChallengeComment = "System: This was an AI Proposed Question, that was not accepted";
                    question.Owner = pbeUser.Email;
                    question.Id = await question.SaveQuestionObjectAsync(context);
                    question.BibleId = bibleId;
                    question.QuestionSelected = true;
                }               //At this point if we've still failed we could resort to an FITB But let's stop here for now. 
            }
            return question;
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
            if (TemplateQuestion.BookNumber == 0)
            {
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
                // We now query for 1 FITB questions in the selected chapter
                // The 4 : 1 ratio is intended to reduce the likelihood of a FITB question popping up. 
                // ordered by longest time since asked,
                // we want to avoid re-asking questions in a short period of time. 
                PossibleQuestions.AddRange(await context.QuizQuestions.Where(Q => Q.BookNumber == BookNumber
                                                                    && (Q.BibleId == bibleId || Q.BibleId == null)
                                                                    && Q.Chapter == Chapter
                                                                    && Q.Challenged == false
                                                                    && Q.IsAnswered == true
                                                                    && !(Q.IsDeleted)
                                                                    && Q.Type == (int)QuestionType.FITB)
                                                                .OrderBy(Q => Q.LastAsked).Take(1).ToListAsync()
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
                // To help our caller since we've selected a book and chapter we'll return those.
                ReturnQuestion.BibleId = bibleId;
                ReturnQuestion.BookNumber = BookNumber;
                ReturnQuestion.Chapter = Chapter;
                ReturnQuestion.QuestionSelected = false;
                return ReturnQuestion;
            }

            if (PossibleQuestions.Count == 0)
            {
                // This is another couldn't find a question scenario.
                // To help our caller since we've selected a book and chapter we'll return those.
                ReturnQuestion.BibleId = bibleId;
                ReturnQuestion.BookNumber = BookNumber;
                ReturnQuestion.Chapter = Chapter;
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
            // To help our caller since we've selected a book and chapter we'll return those.
            ReturnQuestion.BibleId = bibleId;
            ReturnQuestion.BookNumber = BookNumber;
            ReturnQuestion.Chapter = Chapter;
            ReturnQuestion.QuestionSelected = false;
            return ReturnQuestion;
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
                    chapterStat.QuestionStats = new List<QuizQuestionStats>();
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
            public List<QuizQuestionStats> QuestionStats { get; set; }
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
                // Let's add our Question to our Chapter QuestionStats
                QuizQuestionStats QuestionStat = new QuizQuestionStats();
                QuestionStat.QuestionId = question.Id;
                QuestionStats.Add(QuestionStat);
                // Now let's go update this chapterStat
                QuestionStat.AddQuestionToQuestionStat(stat, question);

                // Now let's deal with ChapterStats
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
        public class QuizQuestionStats
        {
            public int QuestionId { get; set; }
            public int PointsPossible { get; set; }
            public int PointsAwarded { get; set; }
            public string QuestionType { get; set; }
            public int VerseNum { get; set; }
            public string VerseSectionName { get; set; }
            public string QuestionPath { get; set; }
            public QuizQuestionStats()
            {
                PointsPossible = 0;
                PointsAwarded = 0;
                QuestionType = Models.QuestionType.Standard.ToString();
                VerseNum = 0;
            }

            public void AddQuestionToQuestionStat(QuizQuestionStat stat, QuizQuestion question)
            {
                if (stat.Points.HasValue) { PointsAwarded = (int)stat.Points; }
                if (question.Chapter == Bible.CommentaryChapter)
                {
                    // Let's grab the title of the first verse as there should be only one
                    VerseSectionName = question.Verses[0].SectionTitle;
                }
                else { VerseSectionName = question.EndVerse.ToString(); }
                PointsPossible += question.Points;
                QuestionType = question.Type.ToString();
                VerseNum = question.EndVerse;
            }
        }
        public class QuestionHistory
        {
            public MinQuestion Question { get; set; }
            public int PointsAwarded { get; set; }
            public int QuestionNumber { get; set; }
            public QuestionHistory()
            {
                PointsAwarded = 0;
                QuestionNumber = 0;
            }

            public void AddQuestionToQuestionHistory(QuizQuestionStat stat, QuizQuestion question)
            {
                if (stat.Points.HasValue) { PointsAwarded = (int)stat.Points; }
                Question = new MinQuestion(question);
            }
        }
    }
}
