using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class CommentaryBooks
    {
        public int Id { get; set; }
        public string BibleId { get; set; }
        public int BookNumber { get; set; }
        public string BookName { get; set; }
        public string Text { get; set; }

        public virtual Bibles Bible { get; set; }
    }
}
