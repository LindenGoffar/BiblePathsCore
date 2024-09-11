using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB;

public partial class PredefinedQuiz
{
    public int Id { get; set; }

    public int? QuizUserId { get; set; }

    public string QuizName { get; set; }

    public int BookNumber { get; set; }

    public int NumQuestions { get; set; }

    public DateTimeOffset? Created { get; set; }

    public DateTimeOffset? Modified { get; set; }

    public bool IsDeleted { get; set; }

    public int Type { get; set; }

    public virtual ICollection<PredefinedQuizQuestion> PredefinedQuizQuestions { get; set; } = new List<PredefinedQuizQuestion>();

    public virtual QuizUser QuizUser { get; set; }
}
