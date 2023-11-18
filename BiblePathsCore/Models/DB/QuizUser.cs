using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB;

public partial class QuizUser
{
    public int Id { get; set; }

    public string Email { get; set; }

    public bool IsQuestionBuilderLocked { get; set; }

    public bool IsQuizTakerLocked { get; set; }

    public bool IsModerator { get; set; }

    public DateTimeOffset? Added { get; set; }

    public DateTimeOffset? Modified { get; set; }

    public virtual ICollection<PredefinedQuiz> PredefinedQuizzes { get; set; } = new List<PredefinedQuiz>();

    public virtual ICollection<QuizGroupStat> QuizGroupStats { get; set; } = new List<QuizGroupStat>();

    public virtual ICollection<QuizQuestionStat> QuizQuestionStats { get; set; } = new List<QuizQuestionStat>();
}
