using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class QuizUsers
    {
        public QuizUsers()
        {
            QuizGroupStats = new HashSet<QuizGroupStats>();
            QuizQuestionStats = new HashSet<QuizQuestionStats>();
        }

        public int Id { get; set; }
        public string Email { get; set; }
        public bool IsQuestionBuilderLocked { get; set; }
        public bool IsQuizTakerLocked { get; set; }
        public bool IsModerator { get; set; }
        public DateTimeOffset? Added { get; set; }
        public DateTimeOffset? Modified { get; set; }

        public virtual ICollection<QuizGroupStats> QuizGroupStats { get; set; }
        public virtual ICollection<QuizQuestionStats> QuizQuestionStats { get; set; }
    }
}
