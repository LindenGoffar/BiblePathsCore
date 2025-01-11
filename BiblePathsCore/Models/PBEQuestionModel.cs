using BiblePathsCore.Services;
using Humanizer.Localisation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Options;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BiblePathsCore.Models
{
    // Enums generally used in the PBE Quiz App
    public enum QuestionType { Standard, FITB, Exclusion, AIProposed}
}

namespace BiblePathsCore.Models.DB
{
    public partial class QuizQuestion
    {
        public enum QuestionEventType { CorrectAnswer, WrongAnswer, QuestionAdd, QuestionPointsAwarded, QuestionAPIToken }

        public const int MaxPoints = 15;
        public const int MinBlankWordLength = 3;
        public const string FITBString = "fill in the blanks";
        public const string FITBBlankWord = "___";

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
        public string CommentarySectionTitle { get; set; }
        [NotMapped]
        public string LegalNote { get; set; }

        public static async Task<List<QuizQuestion>> GetQuestionListAsync(BiblePathsCoreDbContext context, string BibleId, int BookNumber, int Chapter, bool includeChallenged)
        {
            if (includeChallenged)
            {
                return await context.QuizQuestions.Include(Q => Q.QuizAnswers)
                                                        .Where(Q => (Q.BibleId == BibleId || Q.BibleId == null)
                                                                && Q.BookNumber == BookNumber
                                                                && Q.Chapter == Chapter
                                                                && Q.IsDeleted == false
                                                                && (Q.Type == (int)QuestionType.Standard || Q.Type == (int)QuestionType.FITB)
                                                                && Q.IsAnswered == true).ToListAsync();
            }
            
            return await context.QuizQuestions.Include(Q => Q.QuizAnswers)
                                                        .Where(Q => (Q.BibleId == BibleId || Q.BibleId == null)
                                                                && Q.BookNumber == BookNumber
                                                                && Q.Chapter == Chapter
                                                                && Q.IsDeleted == false
                                                                && Q.Challenged == false
                                                                && (Q.Type == (int)QuestionType.Standard || Q.Type == (int)QuestionType.FITB)
                                                                && Q.IsAnswered == true).ToListAsync();
        }

        public static async Task<List<QuizQuestion>> GetQuestionListAsync(BiblePathsCoreDbContext context, string BibleId, int BookNumber, int Chapter, int EndVerse, bool includeChallenged)
        {
            if (includeChallenged)
            {
                return await context.QuizQuestions.Include(Q => Q.QuizAnswers)
                                            .Where(Q => (Q.BibleId == BibleId || Q.BibleId == null)
                                                    && Q.BookNumber == BookNumber
                                                    && Q.Chapter == Chapter
                                                    && Q.EndVerse == EndVerse
                                                    && Q.IsDeleted == false
                                                    && (Q.Type == (int)QuestionType.Standard || Q.Type == (int)QuestionType.FITB)
                                                    && Q.IsAnswered == true).ToListAsync();
            }
            
            return await context.QuizQuestions.Include(Q => Q.QuizAnswers)
                                                        .Where(Q => (Q.BibleId == BibleId || Q.BibleId == null)
                                                                && Q.BookNumber == BookNumber
                                                                && Q.Chapter == Chapter
                                                                && Q.EndVerse == EndVerse
                                                                && Q.IsDeleted == false
                                                                && Q.Challenged == includeChallenged
                                                                && (Q.Type == (int)QuestionType.Standard || Q.Type == (int)QuestionType.FITB)
                                                                && Q.IsAnswered == true).ToListAsync();
        }

        public static async Task<List<QuizQuestion>> GetQuestionOnlyListAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            return await context.QuizQuestions.Where(Q => (Q.BibleId == BibleId || Q.BibleId == null)
                                                                && Q.IsDeleted == false
                                                                && (Q.Type == (int)QuestionType.Standard
                                                                    || Q.Type == (int)QuestionType.FITB))
                                                        .ToListAsync();
        }
        public static async Task<List<QuizQuestion>> GetQuestionOnlyListAsync(BiblePathsCoreDbContext context, string BibleId, bool recent = true)
        {
            DateTime EighteenMonthsAgo = DateTime.Now.AddMonths(-18);
            if (recent)
            {
                return await context.QuizQuestions.Where(Q => (Q.BibleId == BibleId || Q.BibleId == null)
                                                    && Q.IsDeleted == false
                                                    && Q.Modified >= EighteenMonthsAgo
                                                    && (Q.Type == (int)QuestionType.Standard
                                                        || Q.Type == (int)QuestionType.FITB))
                                            .ToListAsync();
            }
            else
            {
                return await context.QuizQuestions.Where(Q => (Q.BibleId == BibleId || Q.BibleId == null)
                                                    && Q.IsDeleted == false
                                                    && (Q.Type == (int)QuestionType.Standard
                                                        || Q.Type == (int)QuestionType.FITB))
                                            .ToListAsync();
            }
        }
        public static async Task<List<QuizQuestion>> GetQuestionOnlyListAsync(BiblePathsCoreDbContext context, string BibleId, int BookNumber)
        {
            return await context.QuizQuestions
                        .Where(Q => (Q.BibleId == BibleId  || Q.BibleId == null)
                                && Q.BookNumber == BookNumber 
                                && Q.IsDeleted == false
                                && (Q.Type == (int)QuestionType.Standard
                                   || Q.Type == (int)QuestionType.FITB))
                        .ToListAsync();
        }

        // TODO: This method assumes Verses is populated, that's not a great assumption 
        // we should remove this dependency. 
        public void PopulatePBEQuestionInfo(BibleBook PBEBook)
        {
            if (Chapter == Bible.CommentaryChapter)
            {
                IsCommentaryQuestion = true;
                BookName = PBEBook.CommentaryTitle;
                // This seems a little risky but in the commentary scenario we try to force a single verse. 
                if (Verses != null)
                {
                    CommentarySectionTitle = Verses.FirstOrDefault().SectionTitle;
                }
                else { CommentarySectionTitle = ""; }
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

        public async Task<bool> UpdateLastAskedAsync(BiblePathsCoreDbContext context)
        {
            // Update this Question Object
            context.Attach(this);
            Type = DetectQuestionType();
            LastAsked = DateTime.Now;
            await context.SaveChangesAsync();
            return true;
        }

        public bool HasQuestionMaintenanceIssue()
        {
            bool retVal = false;

            if(QuizAnswers == null && IsAnswered == true)
            {
                retVal = true;
            }
            else
            {
                if(QuizAnswers.Count == 0 && IsAnswered == true)
                {
                    retVal= true;
                }
                if(QuizAnswers.Count > 1 && IsAnswered == false)
                {
                    retVal = true;
                }
            }

            if(Type != DetectQuestionType())
            {
                retVal = true;
            }

            return retVal;
        }
        public async Task<bool> PerformQuestionMaintenanceTasksAsync(BiblePathsCoreDbContext context)
        {
            // there are a few states on a question object that are presumed not good. 
            // we will check for these here and clean them up. 

            bool Answered = false;
            int Detectedtype = 0;

            // Set IsAnswered as appropriate. 
            if (this.QuizAnswers == null)
            {
                // Load our possible answers. 
                await context.Entry(this)
                    .Collection(Q => Q.QuizAnswers)
                    .LoadAsync();
            }
            if (this.QuizAnswers != null) // still null? Then there are no answers. 
            {
                if (QuizAnswers.Count > 0)
                {
                    Answered = true;
                }
                else { Answered = false; }

            }
            else { Answered = false; }

            // Set QuestionType as appropriate: 
            Detectedtype = DetectQuestionType();

            // Now let's compare agains the real vaues to determine if we need to do a write. 
            if (Answered != this.IsAnswered || Detectedtype != this.Type)
            {
                context.Attach(this);
                IsAnswered = Answered;
                Type = DetectQuestionType();
                await context.SaveChangesAsync();
            }


            return true;
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

        // Save a Built Question to the Database
        public async Task<int> SaveQuestionObjectAsync(BiblePathsCoreDbContext context)
        {
            // The assumption here is that we have a valid Question Object 
            context.QuizQuestions.Add(this);
            await context.SaveChangesAsync();
            return this.Id;
        }

        // This method attempts to determine whether a question is Standard or FITB
        // and will return the appropriate QuestionType. Default being Standard.
        public int DetectQuestionType()
        {
            StringComparison comp = StringComparison.OrdinalIgnoreCase;
            if (this.Question != null)
            {
                if (this.Question.Contains(FITBString, comp)
                        || this.Question.Contains(FITBBlankWord))
                {
                    return (int)QuestionType.FITB;
                }
            }
            return (int)QuestionType.Standard;
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
                tempstring += "According to the " + BookName + ", " + CommentarySectionTitle;
            }
            else
            {
                tempstring += "According to " + BookName + " " + Chapter + ":" + StartVerse;
                if (EndVerse > StartVerse) { tempstring += "-" + EndVerse; }
            }
            tempstring += ", " + Question;
            return tempstring;
        }

        // The primary user of this is the commentary Scenario where we needed
        // a cheaper way to go grab metadata importantly SectionTitle
        public async Task<List<BibleVerse>> GetCommentaryMetadataAsVersesAsync(BiblePathsCoreDbContext context, bool inQuestionOnly)
        {
            List<BibleVerse> bibleVerses = new List<BibleVerse>();

            if (Chapter != Bible.CommentaryChapter)
            {
                return null; // This is explicitly for Commentary Scenarios 
            }
            else // COMMENTARY SCENARIO:
            {
                List<CommentaryBook> commentarySections = new();
                if (inQuestionOnly)
                {
                    // Grab only the section associated with StartVerse
                    // if there are multiple that's not so cool,
                    // but we try to protect from that in Add/EditQuestion. 
                    commentarySections = await context.CommentaryBooks.Where(c => c.BibleId == BibleId
                                                                                && c.BookNumber == BookNumber
                                                                                && c.SectionNumber == StartVerse)
                                                                      .ToListAsync();
                }
                else
                {
                    // Go Get all the Commentary Sections (aka CommentaryBooks) for this book/bible combo. 
                    commentarySections = await context.CommentaryBooks.Where(c => c.BibleId == BibleId
                                                                             && c.BookNumber == BookNumber)
                                                                       .ToListAsync();
                }

                foreach (CommentaryBook Section in commentarySections)
                {
                    BibleVerse CommentaryVerse = new BibleVerse // Verse == Section it's all the same here. 
                    {
                        BibleId = BibleId,
                        BookName = BookName,
                        BookNumber = BookNumber,
                        Chapter = Chapter,
                        Verse = Section.SectionNumber,
                        SectionTitle = Section.SectionTitle
                        // Text = Section.Text This is the optimization in this method, we don't grab the verse text. 
                    };
                    bibleVerses.Add(CommentaryVerse);
                }
            }
            return bibleVerses;
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

            List<QuizQuestion> Questions = QuestionsAndExclusions.Where(Q => (Q.Type == (int)QuestionType.Standard || Q.Type == (int)QuestionType.FITB)).ToList();
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
                    verse.QuestionCount = verse.GetQuestionCountWithQuestionList(Questions);
                    verse.FITBPct = verse.GetFITBPctWithQuestionList(Questions);

                    verse.IsPBEExcluded = verse.IsVerseInExclusionList(Exclusions);
                }
            }
            else // COMMENTARY SCENARIO:
            {
                List<CommentaryBook> commentarySections = new();
                if (inQuestionOnly)
                {
                    // Grab only the section associated with StartVerse if there are multiple that's not so cool, but we try to protect from that in Add/EditQuestion. 
                    commentarySections = await context.CommentaryBooks.Where(c => c.BibleId == BibleId
                                                                                && c.BookNumber == BookNumber
                                                                                && c.SectionNumber == StartVerse)
                                                                      .ToListAsync();
                }
                else
                {
                    // Go Get all the Commentary Sections (aka CommentaryBooks) for this book/bible combo. 
                    commentarySections = await context.CommentaryBooks.Where(c => c.BibleId == BibleId
                                                                             && c.BookNumber == BookNumber)
                                                                       .ToListAsync();
                }

                foreach (CommentaryBook Section in commentarySections)
                {
                    BibleVerse CommentaryVerse = new BibleVerse // Verse, Section it's all the same here. 
                    {
                        BibleId = BibleId,
                        BookName = BookName,
                        BookNumber = BookNumber,
                        Chapter = Chapter,
                        Verse = Section.SectionNumber,
                        SectionTitle = Section.SectionTitle,
                        Text = Section.Text
                    };
                    CommentaryVerse.QuestionCount = CommentaryVerse.GetQuestionCountWithQuestionList(Questions);
                    CommentaryVerse.FITBPct = CommentaryVerse.GetFITBPctWithQuestionList(Questions);
                    bibleVerses.Add(CommentaryVerse);
                }
            }
            return bibleVerses;
        }


        public async Task<List<int>> GetNonExcludedVerseListForQuestionChapter(BiblePathsCoreDbContext context)
        {
            List<BibleVerse> bibleVerses = new List<BibleVerse>();
            List<int> IncludedVerses = new List<int>();
            // Go grab all of the questions and Exclusions for this Chapter once
            // So we can count them as we iterate through each verse
            List<QuizQuestion> Exclusions = await context.QuizQuestions
                                                        .Where(Q => (Q.BibleId == BibleId || Q.BibleId == null)
                                                                && Q.BookNumber == BookNumber
                                                                && Q.Chapter == Chapter
                                                                && Q.Type == (int)QuestionType.Exclusion
                                                                && Q.IsDeleted == false)
                                                        .ToListAsync();
            if (Chapter != Bible.CommentaryChapter)
            {
                bibleVerses = await context.BibleVerses.Where(v => v.BibleId == BibleId && v.BookNumber == BookNumber && v.Chapter == Chapter).OrderBy(v => v.Verse).ToListAsync();
                foreach (BibleVerse verse in bibleVerses)
                {
                    if (verse.IsVerseInExclusionList(Exclusions) == false) { IncludedVerses.Add(verse.Verse); }
                }
            }
            else // COMMENTARY SCENARIO:
            {
                List<CommentaryBook> commentarySections = new();

                // Go Get all the Commentary Sections (aka CommentaryBooks) for this book/bible combo. 
                commentarySections = await context.CommentaryBooks.Where(c => c.BibleId == BibleId
                                                                             && c.BookNumber == BookNumber)
                                                                       .ToListAsync();
                foreach (CommentaryBook section in commentarySections)
                {
                    IncludedVerses.Add(section.SectionNumber);
                }
            }
            return IncludedVerses;
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

        public async Task<BibleVerse> GetRandomVerseAsync(BiblePathsCoreDbContext context)
        {
            List<int> IncludedVerses = await GetNonExcludedVerseListForQuestionChapter(context);
            int IncludedVerseCount = IncludedVerses.Count;

            // Let's pick a random verse from our List of NonExcludedVerses
            // Now Let's pick a random Verse and hope it's not excluded... 
            Random rand = new Random();
            int verseIndex = rand.Next(0, IncludedVerseCount);
            int VerseNum = IncludedVerses[verseIndex];
            BibleVerse ReturnVerse = await BibleVerse.GetVerseAsync(context, BibleId, BookNumber, Chapter, VerseNum);
            return ReturnVerse; 
        }

        public async Task<QuizQuestion> BuildAIQuestionForVerseAsync(BiblePathsCoreDbContext context, BibleVerse verse, IOpenAIResponder openAIResponder)
        {
            // First let's go query OpenAI
            QandAObj qandAObj = await openAIResponder.GetAIQuestionAsync2(verse.Text);
            if (qandAObj == null)
            {
                return null;
            }
            QuizQuestion NewQuestion = new QuizQuestion();
            QuizAnswer AIAnswer = new QuizAnswer();
            NewQuestion.BibleId = verse.BibleId;
            NewQuestion.Points = (qandAObj.points > 0 && qandAObj.points < 7) ? qandAObj.points : 1;
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

    // The MinQuestion Class is used in two scenarios,
    // 1. to overcome some JSON ReferenceLoop issues that occur
    // when a QuizQuestion is passed back through an API call. 
    // 2. to provide a minimal Question object for the quiz History scenario
    public class MinQuestion
    {
        public const string FITBString = "fill in the blanks";
        public const string FITBBlankWord = "___";
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
        public string TypeName { get; set; }
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
            TypeName = Enum.GetName(typeof(QuestionType), quizQuestion.Type);

            Answers = new List<string>();
            foreach(QuizAnswer Answer in quizQuestion.QuizAnswers)
            {
                Answers.Add(Answer.Answer);
            }
        }

        // This method attempts to determine whether a question is Standard or FITB
        // and will return the appropriate QuestionType. Default being Standard.
        public int DetectQuestionType()
        {
            StringComparison comp = StringComparison.OrdinalIgnoreCase;

            if (this.Question != null)
            {
                if (this.Question.Contains(FITBString, comp)
                        || this.Question.Contains(FITBBlankWord))
                {
                    return (int)QuestionType.FITB;
                }
            }
            return (int)QuestionType.Standard;
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
