using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class PredefinedQuizzes
    {
        public PredefinedQuizzes()
        {
            PredefinedQuizQuestions = new HashSet<PredefinedQuizQuestions>();
        }

        public int Id { get; set; }
        public int? QuizUserId { get; set; }
        public string QuizName { get; set; }
        public int BookNumber { get; set; }
        public int NumQuestions { get; set; }
        public DateTimeOffset? Created { get; set; }
        public DateTimeOffset? Modified { get; set; }
        public bool IsDeleted { get; set; }

        public virtual QuizUsers QuizUser { get; set; }
        public virtual ICollection<PredefinedQuizQuestions> PredefinedQuizQuestions { get; set; }
    }
}
