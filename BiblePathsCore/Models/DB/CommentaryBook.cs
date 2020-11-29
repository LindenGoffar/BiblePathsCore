using System;
using System.Collections.Generic;

#nullable disable

namespace BiblePathsCore.Models.DB
{
    public partial class CommentaryBook
    {
        public int Id { get; set; }
        public string BibleId { get; set; }
        public int BookNumber { get; set; }
        public string BookName { get; set; }
        public string Text { get; set; }

        public virtual Bible Bible { get; set; }
    }
}
