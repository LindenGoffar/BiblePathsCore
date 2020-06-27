using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class Bibles
    {
        public Bibles()
        {
            BibleBooks = new HashSet<BibleBooks>();
            BibleNoiseWords = new HashSet<BibleNoiseWords>();
            BibleVerses = new HashSet<BibleVerses>();
            CommentaryBooks = new HashSet<CommentaryBooks>();
            QuizQuestions = new HashSet<QuizQuestions>();
        }

        public string Id { get; set; }
        public string Language { get; set; }
        public string Version { get; set; }

        public virtual ICollection<BibleBooks> BibleBooks { get; set; }
        public virtual ICollection<BibleNoiseWords> BibleNoiseWords { get; set; }
        public virtual ICollection<BibleVerses> BibleVerses { get; set; }
        public virtual ICollection<CommentaryBooks> CommentaryBooks { get; set; }
        public virtual ICollection<QuizQuestions> QuizQuestions { get; set; }
    }
}
