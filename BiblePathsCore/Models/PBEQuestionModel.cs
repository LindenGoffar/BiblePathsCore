﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class QuizQuestions
    {
        [NotMapped]
        public string BookName { get; set; }
        [NotMapped]
        public string PBEQuestion { get; set; }
        [NotMapped]
        public bool IsCommentaryQuestion { get; set; }
        [NotMapped]
        public List<BibleVerses> Verses { get; set; }

        public void PopulatePBEQuestionInfo(Bibles PBEBible)
        {
            if (Chapter == Bibles.CommentaryChapter)
            {
                IsCommentaryQuestion = true;
                BookName = PBEBible.BibleBooks.Where(B => B.BookNumber == BookNumber).Single().CommentaryTitle;
            }
            else
            {
                IsCommentaryQuestion = false;
                BookName = PBEBible.BibleBooks.Where(B => B.BookNumber == BookNumber).Single().Name;
            }
            PBEQuestion = GetPBEQuestionText();
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

        public async Task<List<BibleVerses>> GetBibleVersesAsync(BiblePathsCoreDbContext context, string bibleId, bool inQuestionOnly)
        {
            List<BibleVerses> bibleVerses = new List<BibleVerses>();
            // First retrieve all of the verses, 
            if (inQuestionOnly)
            {
                bibleVerses = await context.BibleVerses.Where(v => v.BibleId == bibleId && v.BookNumber == BookNumber && v.Chapter == Chapter && v.Verse >= StartVerse && v.Verse <= EndVerse).OrderBy(v => v.Verse).ToListAsync();
            }
            else
            {
                bibleVerses = await context.BibleVerses.Where(v => v.BibleId == bibleId && v.BookNumber == BookNumber && v.Chapter == Chapter).OrderBy(v => v.Verse).ToListAsync();
            }
            foreach (BibleVerses verse in bibleVerses)
            {
                verse.QuestionCount = await verse.GetQuestionCountAsync(context);
            }
            return bibleVerses;
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
    }
}