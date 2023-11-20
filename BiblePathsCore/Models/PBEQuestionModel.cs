using BiblePathsCore.Services;
using Humanizer.Localisation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BiblePathsCore.Models
{
    // Enums generally used in the PBE Quiz App
    public enum QuestionType { Standard, Automated, Exclusion} 
}

namespace BiblePathsCore.Models.DB
{
    public partial class QuizQuestion
    {
        public enum QuestionEventType { CorrectAnswer, WrongAnswer, QuestionAdd, QuestionPointsAwarded, QuestionAPIToken }

        public const int MaxPoints = 15;
        public const int MinBlankWordLength = 3;

        [NotMapped]
        public string BookName { get; set; }
        [NotMapped]
        public string PBEQuestion { get; set; }
        [NotMapped]
        public bool IsCommentaryQuestion { get; set; }
        [NotMapped]
        public bool UserCanEdit { get; set; }
        [NotMapped]
        public bool QuestionSelected { get; set; }
        [NotMapped]
        public int TimeLimit { get; set; }
        [NotMapped]
        public int PointsAwarded { get; set; }
        [NotMapped]
        public List<BibleVerse> Verses { get; set; }
        [NotMapped]
        public string LegalNote { get; set; }

        public void PopulatePBEQuestionInfo(BibleBook PBEBook)
        {
            if (Chapter == Bible.CommentaryChapter)
            {
                IsCommentaryQuestion = true;
                BookName = PBEBook.CommentaryTitle;
            }
            else
            {
                IsCommentaryQuestion = false;
                BookName = PBEBook.Name;
            }
            PBEQuestion = GetPBEQuestionText();

            // BibleId may not be set on every question, particularly old ones, so default it.
            if (BibleId == null) { BibleId = Bible.DefaultPBEBibleId; }

            TimeLimit = (Points * 5) + 20;
            LegalNote = GetBibleLegalNote();
        }

        // PopulatePBEQuestionAndBookInfoAsync is expensive as it builds a new PBE Book everytime, we don't want to call this often. 
        public async Task<bool> PopulatePBEQuestionAndBookInfoAsync(BiblePathsCoreDbContext context)
        {
            // BibleId may not be set on every question, particularly old ones, so default it.
            if (BibleId == null) { BibleId = Bible.DefaultPBEBibleId; }

            BibleBook PBEBook = await BibleBook.GetPBEBookAndChapterAsync(context, BibleId, BookNumber, Chapter);
            if (PBEBook == null) { return false; }

            if (Chapter == Bible.CommentaryChapter)
            {
                IsCommentaryQuestion = true;
                BookName = PBEBook.CommentaryTitle;
            }
            else
            {
                IsCommentaryQuestion = false;
                BookName = PBEBook.Name;
            }
            PBEQuestion = GetPBEQuestionText();

            TimeLimit = (Points * 5) + 20;

            return true;
        }

        public async Task<bool> IsQuestionInExclusionAsync(BiblePathsCoreDbContext context)
        {
            bool RetVal = false;
            RetVal = await context.QuizQuestions.AnyAsync(E => E.Type == (int)QuestionType.Exclusion
                                                        && E.BookNumber == BookNumber
                                                        && E.Chapter == Chapter
                                                        && E.IsDeleted == false
                                                        && E.StartVerse <= StartVerse
                                                        && E.EndVerse >= EndVerse
                                                        );
            return RetVal;
        }
        public string GetBibleLegalNote()
        {
            string LegalNote = "";
            if (BibleId == "NKJV-EN")
            {
                LegalNote = "Scripture taken from the New King James Version®. Copyright © 1982 by Thomas Nelson. Used by permission. All rights reserved.";
            }
            return LegalNote;
        }

        public void CheckUserCanEdit(QuizUser PBEUser)
        {
            UserCanEdit =  (Owner == PBEUser.Email || PBEUser.IsModerator);
        }
        private string GetPBEQuestionText()
        {
            string tempstring;
            tempstring = "(" + Points;
            if (Points > 1) { tempstring += "pts) "; }
            else { tempstring += "pt) "; }
            // Handle the Commentary scenario
            if (IsCommentaryQuestion)
            {
                tempstring += "According to the " + BookName;
            }
            else
            {
                tempstring += "According to " + BookName + " " + Chapter + ":" + StartVerse;
                if (EndVerse > StartVerse) { tempstring += "-" + EndVerse; }
            }
            tempstring += ", " + Question;
            return tempstring;
        }

        public async Task<List<BibleVerse>> GetBibleVersesAsync(BiblePathsCoreDbContext context, bool inQuestionOnly)
        {
            List<BibleVerse> bibleVerses = new List<BibleVerse>();
            // Go grab all of the questions and Exclusions for this Chapter once
            // So we can count them as we iterate through each verse
            List<QuizQuestion> QuestionsAndExclusions = await context.QuizQuestions
                                                        .Where(Q => (Q.BibleId == BibleId || Q.BibleId == null)
                                                                && Q.BookNumber == BookNumber
                                                                && Q.Chapter == Chapter
                                                                //&& Q.Type == (int)QuestionType.Standard
                                                                && Q.IsDeleted == false)
                                                        .ToListAsync();

            List<QuizQuestion> Questions = QuestionsAndExclusions.Where(Q => Q.Type == (int)QuestionType.Standard).ToList();
            List<QuizQuestion> Exclusions = QuestionsAndExclusions.Where(Q => Q.Type == (int)QuestionType.Exclusion).ToList();

            if (Chapter != Bible.CommentaryChapter)
            {
                // First retrieve all of the verses, 
                if (inQuestionOnly)
                {
                    bibleVerses = await context.BibleVerses.Where(v => v.BibleId == BibleId && v.BookNumber == BookNumber && v.Chapter == Chapter && v.Verse >= StartVerse && v.Verse <= EndVerse).OrderBy(v => v.Verse).ToListAsync();
                }
                else
                {
                    bibleVerses = await context.BibleVerses.Where(v => v.BibleId == BibleId && v.BookNumber == BookNumber && v.Chapter == Chapter).OrderBy(v => v.Verse).ToListAsync();
                }
                foreach (BibleVerse verse in bibleVerses)
                {
                    verse.QuestionCount = verse.GetQuestionCountWithQuestionList (Questions);
                    verse.IsPBEExcluded = verse.IsVerseInExclusionList(Exclusions);
                }
            }
            else // COMMENTARY SCENARIO:
            {
                CommentaryBook commentary = await context.CommentaryBooks.Where(c => c.BibleId == BibleId
                                                                            && c.BookNumber == BookNumber)
                                                                          .FirstAsync();
                BibleVerse CommentaryVerse = new BibleVerse
                {
                    BibleId = BibleId,
                    BookName = BookName,
                    BookNumber = BookNumber,
                    Chapter = Chapter,
                    Verse = 1,
                    Text = commentary.Text,
                };
                bibleVerses.Add(CommentaryVerse);
            }
            return bibleVerses;
        }

        public List<SelectListItem> GetPointsSelectList()
        {
            List<SelectListItem> PointsSelectList = new List<SelectListItem>();
            for (int i = 1; i <= MaxPoints; i++)
            {
                PointsSelectList.Add(new SelectListItem
                {
                    Text = i.ToString(),
                    Value = i.ToString(),
                });

            }
            return PointsSelectList;
        }

        public List<SelectListItem> GetQuestionPointsSelectList()
        {
            List<SelectListItem> PointsSelectList = new List<SelectListItem>();
            PointsSelectList.Add(new SelectListItem
            {
                Text = "Points",
                Value = "-1",
                Selected = true,
            });
            for (int i = 0; i <= Points; i++)
            {
                PointsSelectList.Add(new SelectListItem
                {
                    Text = i.ToString(),
                    Value = i.ToString(),
                });
            }
            return PointsSelectList;
        }

        public static async Task<string> GetValidBibleIdAsync(BiblePathsCoreDbContext context, string BibleId)
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

        public async Task<QuizQuestion> BuildAIQuestionForVerseAsync(BiblePathsCoreDbContext context, BibleVerse verse, IOpenAIResponder openAIResponder, OpenAISettings openAISettings)
        {
            // First let's go query OpenAI
            QandAObj qandAObj = await openAIResponder.GetAIQuestionAsync(verse.Text, openAISettings.OpenAIAPIKey);
            if (qandAObj == null)
            {
                return null;
            }
            QuizQuestion NewQuestion = new QuizQuestion();
            QuizAnswer AIAnswer = new QuizAnswer();
            NewQuestion.BibleId = verse.BibleId;
            NewQuestion.Points = 1;
            NewQuestion.Question = qandAObj.question;
            NewQuestion.BookNumber = verse.BookNumber;
            NewQuestion.Chapter = verse.Chapter;
            NewQuestion.StartVerse = verse.Verse;
            NewQuestion.EndVerse = verse.Verse;
            NewQuestion.Source = "BiblePaths.Net OpenAI Question Generator (" + OpenAIResponder.OpenAIAPI + ")";
            // Build the Answer
            AIAnswer.Answer = qandAObj.answer;
            NewQuestion.QuizAnswers.Add(AIAnswer);

            if (NewQuestion.QuizAnswers.Count > 0) { NewQuestion.IsAnswered = true; }
            else { NewQuestion.IsAnswered = false; }

            return NewQuestion;
        }
        public async Task<QuizQuestion> BuildQuestionForVerseAsync(BiblePathsCoreDbContext context, BibleVerse verse, int MaxPoints, string BibleId)
        {
            int BlankWordProbability = 3; // read as 1 in every 3 valid words may get blanked rnd is 0 based
            int MinPoints = 3;
            int Iteration = 0;
            int MaxIterations = 3;
            string BlankWordSring = "_____";
            string FitBPrepend = "fill in the blanks: ";
            int BlankedWordCount = 0;
            List<string> BlankedWords = new List<string>();
            string QuestionString = String.Empty;

            BibleId = await QuizQuestion.GetValidBibleIdAsync(context, BibleId);
            Random rnd = new Random();

            // We'll make at most MaxIteratons at this, increasing BlankWordProbability each time. 
            while (Iteration < MaxIterations && BlankedWordCount < MinPoints)
            {
                QuestionString = verse.Text;
                BlankedWords.Clear();
                BlankedWordCount = 0;

                // Read the verse "word" by "word" i.e. stop at each Space till we hit the end. 
                int i = 0;
                while (i < QuestionString.Length && BlankedWordCount < MaxPoints)
                {
                    int WordStart = i;
                    int WordEnd = QuestionString.IndexOf(" ", i);
                    if (WordEnd == -1) { WordEnd = QuestionString.Length - 1; }

                    // Now we should have a rough "word" to work with,
                    // but let's progressively shrink our Word Selction until it's starts/ends with a letter.
                    if (WordStart < WordEnd && WordEnd < QuestionString.Length)
                    {
                        // find the first letter
                        while (WordStart < WordEnd && !char.IsLetter(QuestionString, WordStart))
                        {
                            WordStart++;
                        }
                        // find the last letter
                        while (WordEnd > WordStart && !char.IsLetter(QuestionString, WordEnd))
                        {
                            WordEnd--;
                        }

                        // Now we should have a true "word" at least as far as we are concerned. 
                        string Word = QuestionString.Substring(WordStart, (WordEnd + 1) - WordStart);

                        if (await IsWordGoodForBlankAsync(context, Word, BibleId))
                        {
                            // Ok now we don't want to simply replace every valid word so let's get random
                            int dice = rnd.Next(BlankWordProbability);
                            if (dice == 0) // 0 will always turn up on the 3rd iteration. 
                            {
                                // Blank out our word in the QuestionString 
                                StringBuilder VerseWithBlanksSB = new StringBuilder();
                                VerseWithBlanksSB.Append(QuestionString.Substring(0, WordStart));
                                VerseWithBlanksSB.Append(BlankWordSring);
                                VerseWithBlanksSB.Append(QuestionString.Substring(WordEnd + 1));
                                QuestionString = VerseWithBlanksSB.ToString();
                                // Add our word to the blanked words list 
                                BlankedWords.Add(Word);
                                BlankedWordCount++;
                                // Set our index to the latest instance of BlankWordString
                                i = QuestionString.LastIndexOf(BlankWordSring) + BlankWordSring.Length;
                            }
                            else
                            {
                                i = WordEnd + 1;
                            }
                        }
                        else
                        {
                            i = WordEnd + 1;
                        }
                    }
                    else
                    {
                        // Why would we ever be here? 
                        i = WordEnd + 1;
                    }
                }
                Iteration++;
                BlankWordProbability--; // Reducing this increases the probability of any word being selected.
            }
            
            QuizQuestion NewQuestion = new QuizQuestion();
            QuizAnswer FitBAnswer = new QuizAnswer();
            NewQuestion.BibleId = BibleId;
            NewQuestion.Points = BlankedWordCount;
            NewQuestion.Question = FitBPrepend + QuestionString;
            NewQuestion.BookNumber = verse.BookNumber;
            NewQuestion.Chapter = verse.Chapter;
            NewQuestion.StartVerse = verse.Verse;
            NewQuestion.EndVerse = verse.Verse;
            NewQuestion.Source = "BiblePaths.Net FITB Question Generator - Iteration: " + Iteration.ToString();
            // Build the Answer
            FitBAnswer.Answer = string.Join(", ", BlankedWords);
            NewQuestion.QuizAnswers.Add(FitBAnswer);

            if (NewQuestion.QuizAnswers.Count > 0) { NewQuestion.IsAnswered = true; }
            else { NewQuestion.IsAnswered = false; }

            return NewQuestion;
        }

        public async Task<bool> IsWordGoodForBlankAsync(BiblePathsCoreDbContext context, string CheckWord, string BibleId)
        {
            if (!string.IsNullOrEmpty(CheckWord))
            {
                if (CheckWord.Length >= QuizQuestion.MinBlankWordLength)
                {
                    // See if we can find this word in our NoiseWord DB, and make sure it's not flagged as noise.
                    try
                    {
                        bool retVal = await context.BibleNoiseWords.Where(W => W.NoiseWord == CheckWord
                                                                   && W.IsNoise == false
                                                                   && W.BibleId == BibleId)
                                                            .AnyAsync();
                        return retVal;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        public async Task<bool> RegisterEventAsync(BiblePathsCoreDbContext context, QuestionEventType questionEventType, int QuizUserId, string EventData, int? QuizId, int? Points)
        {
            QuizQuestionStat stat = new QuizQuestionStat
            {
                QuestionId = Id,
                QuizUserId = QuizUserId,
                QuizGroupId = QuizId,
                EventType = (int)questionEventType,
                EventData = EventData,
                Points = Points,
                EventWritten = DateTime.Now
            };
            context.QuizQuestionStats.Add(stat);
            if (await context.SaveChangesAsync() == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    // The MinQuestion Class is used to overcome some JSON ReferenceLoop issues that occur
    // when a QuizQuestion is passed back through an API call. 
    public class MinQuestion
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string PBEQuestion { get; set; }
        public bool IsAnswered { get; set; }
        public int Points { get; set; }
        public int BookNumber { get; set; }
        public string BookName { get; set; }
        public int Chapter { get; set; }
        public int StartVerse { get; set; }
        public int EndVerse { get; set; }
        public DateTimeOffset? Created { get; set; }
        public DateTimeOffset? Modified { get; set; }
        public string Source { get; set; }
        public DateTimeOffset LastAsked { get; set; }
        public string BibleId { get; set; }
        public List<string> Answers { get; set; }
        public bool IsCommentaryQuestion { get; set; }
        public string Owner { get; set; }
        public string Token { get; set; }
        public MinQuestion()
        {
            // Parameterless constructor required for Post Action. 
        }
        public MinQuestion(QuizQuestion quizQuestion)
        {
            Id = quizQuestion.Id;
            Question = quizQuestion.Question;
            PBEQuestion = quizQuestion.PBEQuestion;
            IsAnswered = quizQuestion.IsAnswered;
            Points = quizQuestion.Points;
            BookNumber = quizQuestion.BookNumber;
            BookName = quizQuestion.BookName;
            Chapter = quizQuestion.Chapter;
            StartVerse = quizQuestion.StartVerse;
            EndVerse = quizQuestion.EndVerse;
            Created = quizQuestion.Created;
            Modified = quizQuestion.Modified;
            Source = quizQuestion.Source;
            LastAsked = quizQuestion.LastAsked;
            BibleId = quizQuestion.BibleId;
            IsCommentaryQuestion = quizQuestion.IsCommentaryQuestion;

            Answers = new List<string>();
            foreach(QuizAnswer Answer in quizQuestion.QuizAnswers)
            {
                Answers.Add(Answer.Answer);
            }
        }

        public async Task<bool> APIUserTokenCheckAsync(BiblePathsCoreDbContext context)
        {
            // Do we have a valid Owner value:
            if (await QuizUser.IsValidPBEQuestionOwnerAsync(context, this.Owner) == false)
            {
                return false;
            }
            else
            {
                QuizUser PBEUser = await QuizUser.GetPBEUserAsync(context, this.Owner); // Static method not requiring an instance
                if (PBEUser.IsQuestionBuilderLocked) { return false; }
                if (await PBEUser.CheckAPITokenAsync(context, this.Token) == true)
                {
                    return true;
                }
            }
            return false; 
        }
    }

}
