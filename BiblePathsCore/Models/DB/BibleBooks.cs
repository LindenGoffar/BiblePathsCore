using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class BibleBooks
    {
        public BibleBooks()
        {
            BibleChapters = new HashSet<BibleChapters>();
        }

        public string BibleId { get; set; }
        public string Testament { get; set; }
        public int? TestamentNumber { get; set; }
        public int BookNumber { get; set; }
        public string Name { get; set; }
        public int? Chapters { get; set; }

        public virtual Bibles Bible { get; set; }
        public virtual ICollection<BibleChapters> BibleChapters { get; set; }
    }
}
