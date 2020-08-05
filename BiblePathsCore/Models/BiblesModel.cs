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
    public partial class Bibles
    {
        public const string DefaultBibleId = "KJV-EN";
        public const string DefaultPBEBibleId = "NKJV-EN";
        public const int MinBookListID = 1000; // PBE: all book lists start here to ensure no conflict with books
        public const int CommentaryChapter = 1000; // PBE: A chapter number of "CommentaryChapter" indicates a Commentary rather then actual Bible Chapter

        [NotMapped]
        public string LegalNote { get; set; }

        public static async Task<Bibles> GetPBEBibleAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            // TODO PERF: This is a way expensive method we need to reduce calls to this. 

            Bibles PBEBible = await context.Bibles.Include(B => B.BibleBooks)
                                                  .ThenInclude(Book => Book.BibleChapters)
                                                  .Where(B => B.Id == BibleId).SingleAsync();
            if (PBEBible == null) { return null; }

            PBEBible.HydrateBible();
            // A PBE Bible has all of the Books and chapters loaded and marked up with PBE info. 
            foreach (BibleBooks Book in PBEBible.BibleBooks)
            {
                await Book.AddPBEBookPropertiesAsync(context, null);

                // This should already be done for us in AddPBEBookPropertiesAsync
                //foreach (BibleChapters Chapter in Book.BibleChapters)
                //{
                //    await Chapter.AddPBEChapterPropertiesAsync(context);
                //}
            }
            return PBEBible; 
        }

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

    }
}
