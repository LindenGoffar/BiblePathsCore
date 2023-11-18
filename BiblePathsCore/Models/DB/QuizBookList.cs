using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB;

public partial class QuizBookList
{
    public int Id { get; set; }

    public string BookListName { get; set; }

    public DateTimeOffset? Created { get; set; }

    public DateTimeOffset? Modified { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<QuizBookListBookMap> QuizBookListBookMaps { get; set; } = new List<QuizBookListBookMap>();
}
