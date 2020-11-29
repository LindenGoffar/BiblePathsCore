using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class QuizQuestion
    {
        public enum QuestionEventType { CorrectAnswer, WrongAnswer, QuestionAdd, QuestionPointsAwarded, QuestionAPIToken }

        public const int MaxPoints = 15;

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
                tempstring += "According to the SDABC for " + BookName;
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
            // Go grab all of the questions for this Chapter once
            // So we can count them as we iterate through each verse
            List<QuizQuestion> Questions = await context.QuizQuestions
                                                        .Where(Q => (Q.BibleId == BibleId || Q.BibleId == null)
                                                                && Q.BookNumber == BookNumber
                                                                && Q.Chapter == Chapter
                                                                && Q.IsDeleted == false)
                                                        .ToListAsync();

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
            if (await QuizUser.IsValidPBEUserAsync(context, this.Owner) == false)
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
