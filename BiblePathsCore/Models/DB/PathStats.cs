using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class PathStats
    {
        public int Id { get; set; }
        public int? PathId { get; set; }
        public int EventType { get; set; }
        public string EventData { get; set; }
        public DateTimeOffset? EventWritten { get; set; }

        public virtual Paths Path { get; set; }
    }
}
