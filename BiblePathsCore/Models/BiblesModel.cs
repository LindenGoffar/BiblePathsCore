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

        public MinBible(Bibles Bible)
        {
            LegalNote = Bible.LegalNote;
            Id = Bible.Id;
            Language = Bible.Language;
            Version = Bible.Version;
            BibleBooks = new List<MinBook>();
            foreach(BibleBooks Book in Bible.BibleBooks)
            {
                MinBook minBook = new MinBook(Book);
                BibleBooks.Add(minBook);
            }
        }
    }
}
