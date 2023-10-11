using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class Bible
    {
        public Bible()
        {
            BibleBooks = new HashSet<BibleBook>();
            BibleNoiseWords = new HashSet<BibleNoiseWord>();
            BibleVerses = new HashSet<BibleVerse>();
            BibleWordIndices = new HashSet<BibleWordIndex>();
            CommentaryBooks = new HashSet<CommentaryBook>();
            QuizQuestions = new HashSet<QuizQuestion>();
        }

        public string Id { get; set; }
        public string Language { get; set; }
        public string Version { get; set; }
        public int Type { get; set; }

        public virtual ICollection<BibleBook> BibleBooks { get; set; }
        public virtual ICollection<BibleNoiseWord> BibleNoiseWords { get; set; }
        public virtual ICollection<BibleVerse> BibleVerses { get; set; }
        public virtual ICollection<BibleWordIndex> BibleWordIndices { get; set; }
        public virtual ICollection<CommentaryBook> CommentaryBooks { get; set; }
        public virtual ICollection<QuizQuestion> QuizQuestions { get; set; }
    }
}
