using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB;

public partial class BibleWordIndex
{
    public int Id { get; set; }

    public string BibleId { get; set; }

    public string Word { get; set; }

    public int VerseId { get; set; }

    public int BookNumber { get; set; }

    public int Chapter { get; set; }

    public int Verse { get; set; }

    public int RandomInt { get; set; }

    public virtual Bible Bible { get; set; }
}
