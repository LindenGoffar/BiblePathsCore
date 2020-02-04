using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class QuizAnswers
    {
        public int Id { get; set; }
        public int? QuestionId { get; set; }
        public string Answer { get; set; }
        public bool IsPrimary { get; set; }
        public DateTimeOffset? Created { get; set; }
        public DateTimeOffset? Modified { get; set; }

        public virtual QuizQuestions Question { get; set; }
    }
}
