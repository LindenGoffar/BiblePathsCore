﻿using System;
using System.Collections.Generic;

#nullable disable

namespace BiblePathsCore.Models.DB
{
    public partial class BibleChapter
    {
        public string BibleId { get; set; }
        public int BookNumber { get; set; }
        public string Name { get; set; }
        public int ChapterNumber { get; set; }
        public int? Verses { get; set; }

        public virtual BibleBook B { get; set; }
    }
}