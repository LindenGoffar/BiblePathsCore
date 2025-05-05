using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB;

public partial class BibleVerseTongue
{
    public int Id { get; set; }

    public string FromBibleId { get; set; }

    public string FromLanguage { get; set; }

    public string ToLanguage { get; set; }

    public int VerseId { get; set; }

    public int BookNumber { get; set; }

    public int Chapter { get; set; }

    public int Verse { get; set; }

    public string TonguesJson { get; set; }

    public DateTimeOffset? Created { get; set; }

    public DateTimeOffset? Modified { get; set; }

    public virtual Bible FromBible { get; set; }
}
