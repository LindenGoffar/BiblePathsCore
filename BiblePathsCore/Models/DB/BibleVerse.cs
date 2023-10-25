using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB;

public partial class BibleVerse
{
    public int Id { get; set; }

    public string BibleId { get; set; }

    public string Testament { get; set; }

    public int BookNumber { get; set; }

    public string BookName { get; set; }

    public int Chapter { get; set; }

    public int Verse { get; set; }

    public string Text { get; set; }

    public virtual Bible Bible { get; set; }
}
