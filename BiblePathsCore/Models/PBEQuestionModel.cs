using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class QuizQuestions
    {
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
        public List<BibleVerses> Verses { get; set; }

        public void PopulatePBEQuestionInfo(BibleBooks PBEBook)
        {
            if (Chapter == Bibles.CommentaryChapter)
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
            if (BibleId == null) { BibleId = Bibles.DefaultPBEBibleId; }
        }

        public void CheckUserCanEdit(QuizUsers PBEUser)
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

        public async Task<List<BibleVerses>> GetBibleVersesAsync(BiblePathsCoreDbContext context, bool inQuestionOnly)
        {
            List<BibleVerses> bibleVerses = new List<BibleVerses>();
            // First retrieve all of the verses, 
            if (inQuestionOnly)
            {
                bibleVerses = await context.BibleVerses.Where(v => v.BibleId == BibleId && v.BookNumber == BookNumber && v.Chapter == Chapter && v.Verse >= StartVerse && v.Verse <= EndVerse).OrderBy(v => v.Verse).ToListAsync();
            }
            else
            {
                bibleVerses = await context.BibleVerses.Where(v => v.BibleId == BibleId && v.BookNumber == BookNumber && v.Chapter == Chapter).OrderBy(v => v.Verse).ToListAsync();
            }
            foreach (BibleVerses verse in bibleVerses)
            {
                verse.QuestionCount = await verse.GetQuestionCountAsync(context);
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

        public static async Task<string> GetValidBibleIdAsync(BiblePathsCoreDbContext context, string BibleId)
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
    }
}
