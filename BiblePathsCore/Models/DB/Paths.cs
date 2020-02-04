using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class Paths
    {
        public Paths()
        {
            PathNodes = new HashSet<PathNodes>();
            PathStats = new HashSet<PathStats>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int Length { get; set; }
        public decimal? ComputedRating { get; set; }
        public DateTimeOffset? Created { get; set; }
        public DateTimeOffset? Modified { get; set; }
        public string Owner { get; set; }
        public string OwnerBibleId { get; set; }
        public string Topics { get; set; }
        public bool IsPublished { get; set; }
        public bool IsPublicEditable { get; set; }
        public bool IsDeleted { get; set; }
        public int StepCount { get; set; }
        public int Reads { get; set; }

        public virtual ICollection<PathNodes> PathNodes { get; set; }
        public virtual ICollection<PathStats> PathStats { get; set; }
    }
}
