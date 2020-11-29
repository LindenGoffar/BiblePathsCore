using System;
using System.Collections.Generic;

#nullable disable

namespace BiblePathsCore.Models.DB
{
    public partial class QuizUser
    {
        public QuizUser()
        {
            PredefinedQuizzes = new HashSet<PredefinedQuiz>();
            QuizGroupStats = new HashSet<QuizGroupStat>();
            QuizQuestionStats = new HashSet<QuizQuestionStat>();
        }

        public int Id { get; set; }
        public string Email { get; set; }
        public bool IsQuestionBuilderLocked { get; set; }
        public bool IsQuizTakerLocked { get; set; }
        public bool IsModerator { get; set; }
        public DateTimeOffset? Added { get; set; }
        public DateTimeOffset? Modified { get; set; }

        public virtual ICollection<PredefinedQuiz> PredefinedQuizzes { get; set; }
        public virtual ICollection<QuizGroupStat> QuizGroupStats { get; set; }
        public virtual ICollection<QuizQuestionStat> QuizQuestionStats { get; set; }
    }
}
