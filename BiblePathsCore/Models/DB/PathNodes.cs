using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class PathNodes
    {
        public int Id { get; set; }
        public int? PathId { get; set; }
        public int Position { get; set; }
        public int BookNumber { get; set; }
        public int Chapter { get; set; }
        public int StartVerse { get; set; }
        public int EndVerse { get; set; }
        public DateTimeOffset? Created { get; set; }
        public DateTimeOffset? Modified { get; set; }

        public virtual Paths Path { get; set; }
    }
}
