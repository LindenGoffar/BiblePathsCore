using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class QuizQuestions
    {
        public QuizQuestions()
        {
            QuizAnswers = new HashSet<QuizAnswers>();
            QuizQuestionStats = new HashSet<QuizQuestionStats>();
        }

        public int Id { get; set; }
        public string Question { get; set; }
        public string Owner { get; set; }
        public bool Challenged { get; set; }
        public string ChallengedBy { get; set; }
        public string ChallengeComment { get; set; }
        public bool IsAnswered { get; set; }
        public bool IsDeleted { get; set; }
        public int Points { get; set; }
        public int BookNumber { get; set; }
        public int Chapter { get; set; }
        public int StartVerse { get; set; }
        public int EndVerse { get; set; }
        public DateTimeOffset? Created { get; set; }
        public DateTimeOffset? Modified { get; set; }
        public string Source { get; set; }
        public DateTimeOffset LastAsked { get; set; }
        public string BibleId { get; set; }

        public virtual Bibles Bible { get; set; }
        public virtual ICollection<QuizAnswers> QuizAnswers { get; set; }
        public virtual ICollection<QuizQuestionStats> QuizQuestionStats { get; set; }
    }
}
