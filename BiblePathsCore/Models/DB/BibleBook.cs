using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB;

public partial class BibleBook
{
    public string BibleId { get; set; }

    public string Testament { get; set; }

    public int? TestamentNumber { get; set; }

    public int BookNumber { get; set; }

    public string Name { get; set; }

    public int? Chapters { get; set; }

    public virtual Bible Bible { get; set; }

    public virtual ICollection<BibleChapter> BibleChapters { get; set; } = new List<BibleChapter>();
}
