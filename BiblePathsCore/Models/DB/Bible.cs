using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB;

public partial class Bible
{
    public string Id { get; set; }

    public string Language { get; set; }

    public string Version { get; set; }

    public int Type { get; set; }

    public virtual ICollection<BibleBook> BibleBooks { get; set; } = new List<BibleBook>();

    public virtual ICollection<BibleNoiseWord> BibleNoiseWords { get; set; } = new List<BibleNoiseWord>();

    public virtual ICollection<BibleVerse> BibleVerses { get; set; } = new List<BibleVerse>();

    public virtual ICollection<BibleWordIndex> BibleWordIndices { get; set; } = new List<BibleWordIndex>();

    public virtual ICollection<CommentaryBook> CommentaryBooks { get; set; } = new List<CommentaryBook>();

    public virtual ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
}
