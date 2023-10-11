using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class PredefinedQuizQuestion
    {
        public int Id { get; set; }
        public int? PredefinedQuizId { get; set; }
        public int QuestionNumber { get; set; }
        public int BookNumber { get; set; }
        public int Chapter { get; set; }

        public virtual PredefinedQuiz PredefinedQuiz { get; set; }
    }
}
