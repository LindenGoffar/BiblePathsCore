using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB;

public partial class CommentaryBook
{
    public int Id { get; set; }

    public string BibleId { get; set; }

    public int BookNumber { get; set; }

    public string BookName { get; set; }

    public string CommentaryTitle { get; set; }

    public string Owner { get; set; }

    public DateTimeOffset? Created { get; set; }

    public DateTimeOffset? Modified { get; set; }

    public string Text { get; set; }

    public int SectionNumber { get; set; }

    public string SectionTitle { get; set; }

    public virtual Bible Bible { get; set; }
}
