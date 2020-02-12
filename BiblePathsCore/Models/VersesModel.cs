using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class BibleVerses
    {
        [NotMapped]
        public bool InPath { get; set; }
        [NotMapped]
        public int Proximity { get; set; }
    }
}
