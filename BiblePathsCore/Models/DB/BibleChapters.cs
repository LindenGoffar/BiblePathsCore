using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class BibleChapters
    {
        public string BibleId { get; set; }
        public int BookNumber { get; set; }
        public string Name { get; set; }
        public int ChapterNumber { get; set; }
        public int? Verses { get; set; }

        public virtual BibleBooks B { get; set; }
    }
}
