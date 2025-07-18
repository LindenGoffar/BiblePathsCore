using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class Bible
    {
        public const string DefaultBibleId = "KJV-EN";
        public const string DefaultPBEBibleId = "NKJV-EN";
        public const int MinBookListID = 1000; // PBE: all book lists start here to ensure no conflict with books
        public const int CommentaryChapter = 1000; // PBE: A chapter number of "CommentaryChapter" indicates a Commentary rather then actual Bible Chapter

        [NotMapped]
        public string LegalNote { get; set; }

        public bool HydrateBible()
        {
            LegalNote = GetBibleLegalNote();
            return true;
        }

        private string GetBibleLegalNote()
        {
            string LegalNote = "";
            if (Id == "NKJV-EN")
            {
                LegalNote = "Scripture taken from the New King James Version®. Copyright © 1982 by Thomas Nelson. Used by permission. All rights reserved.";
            }
            return LegalNote;
        }

        public static async Task<string> GetValidPBEBibleIdAsync(BiblePathsCoreDbContext context, string BibleId)
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

        public static async Task<string> GetValidBibleIdAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            string RetVal = Bible.DefaultBibleId;
            if (BibleId != null)
            {
                if (await context.Bibles.Where(B => B.Id == BibleId).AnyAsync())
                {
                    RetVal = BibleId;
                }
            }
            return RetVal;
        }
        public static async Task<string> GetBibleIdByLanguagAsync(BiblePathsCoreDbContext context, string Language)
        {
            string RetVal = Bible.DefaultBibleId;
            if (Language != null)
            {
                try
                {
                    RetVal = await context.Bibles.Where(B => B.Language == Language).Select(B => B.Id).FirstAsync();
                }
                catch
                {
                    // If no Bible found for the specified language, we return the default Bible ID
                    RetVal = Bible.DefaultBibleId;
                }
            }
            return RetVal;
        }

        public static async Task<Bible> GetBibleAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            Bible bible = new Bible();
            BibleId = await Bible.GetValidBibleIdAsync(context, BibleId);
            bible = await context.Bibles.Where(B => B.Id == BibleId).FirstOrDefaultAsync();

            return bible;
        }

    }

    public class MinBible
    {
        public string LegalNote { get; set; }
        public string Id { get; set; }
        public string Language { get; set; }
        public string Version { get; set; }
        public List<MinBook> BibleBooks { get; set; }

        public MinBible()
        {

        }

        public MinBible(Bible Bible)
        {
            LegalNote = Bible.LegalNote;
            Id = Bible.Id;
            Language = Bible.Language;
            Version = Bible.Version;
            BibleBooks = new List<MinBook>();
            foreach(BibleBook Book in Bible.BibleBooks)
            {
                MinBook minBook = new MinBook(Book);
                BibleBooks.Add(minBook);
            }
        }
    }
}
