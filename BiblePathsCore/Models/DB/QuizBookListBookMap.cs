using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB;

public partial class QuizBookListBookMap
{
    public int Id { get; set; }

    public int? BookListId { get; set; }

    public int BookNumber { get; set; }

    public DateTimeOffset? Created { get; set; }

    public DateTimeOffset? Modified { get; set; }

    public bool IsDeleted { get; set; }

    public virtual QuizBookList BookList { get; set; }
}
