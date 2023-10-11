using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    // The PBEExclusion class is used in the special cases where questions from specific verses or entire 
    // chapters are excluded from PBE Events
    public class PBEExclusion
    {
        public int Id { get; set; }
        public int BookNumber { get; set; }
        public string BookName { get; set; }
        public int Chapter { get; set; }
        public int StartVerse { get; set; }
        public int EndVerse { get; set; }
        public DateTimeOffset? Created { get; set; }
        public DateTimeOffset? Modified { get; set; }
        public string BibleId { get; set; }
        public string Owner { get; set; }
        public PBEExclusion()
        {
            // Parameterless constructor required for Post Action. 
        }
        public PBEExclusion(QuizQuestion quizQuestion)
        {
            Id = quizQuestion.Id;
            BookNumber = quizQuestion.BookNumber;
            Chapter = quizQuestion.Chapter;
            StartVerse = quizQuestion.StartVerse;
            EndVerse = quizQuestion.EndVerse;
            Created = quizQuestion.Created;
            Modified = quizQuestion.Modified;
            BibleId = quizQuestion.BibleId;
            Owner = quizQuestion.Owner;
        }
        // PopulateExclusionAsync is used to populate BookName, this could become expensive if called too often but 
        // the number of exclusions should be kept relatively low. 
        public async Task<bool> PopulateExclusionAsync(BiblePathsCoreDbContext context)
        {
            BookName = await BibleBook.GetBookNameAsync(context, BibleId, BookNumber);
            return true;
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
        public List<SelectListItem> GetVerseNumSelectList(int VerseCount)
        {
            List<SelectListItem> VerseSelectList = new List<SelectListItem>();
            for (int i = 1; i <= VerseCount; i++)
            {
                VerseSelectList.Add(new SelectListItem
                {
                    Text = i.ToString(),
                    Value = i.ToString(),
                });

            }
            return VerseSelectList;
        }
    }
}
