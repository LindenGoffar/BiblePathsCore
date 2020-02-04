using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class BibleNoiseWords
    {
        public string BibleId { get; set; }
        public string NoiseWord { get; set; }

        public virtual Bibles Bible { get; set; }
    }
}
