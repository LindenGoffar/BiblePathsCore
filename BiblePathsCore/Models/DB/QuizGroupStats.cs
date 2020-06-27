using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class QuizGroupStats
    {
        public int Id { get; set; }
        public int? QuizUserId { get; set; }
        public string GroupName { get; set; }
        public int BookNumber { get; set; }
        public int QuestionsAsked { get; set; }
        public int PointsPossible { get; set; }
        public int PointsAwarded { get; set; }
        public int PredefinedQuiz { get; set; }
        public DateTimeOffset? Created { get; set; }
        public DateTimeOffset? Modified { get; set; }
        public bool IsDeleted { get; set; }

        public virtual QuizUsers QuizUser { get; set; }
    }
}
