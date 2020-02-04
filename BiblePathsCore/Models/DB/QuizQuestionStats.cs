using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class QuizQuestionStats
    {
        public int Id { get; set; }
        public int? QuestionId { get; set; }
        public int? QuizUserId { get; set; }
        public int? QuizGroupId { get; set; }
        public int? Points { get; set; }
        public int EventType { get; set; }
        public string EventData { get; set; }
        public DateTimeOffset? EventWritten { get; set; }

        public virtual QuizQuestions Question { get; set; }
        public virtual QuizUsers QuizUser { get; set; }
    }
}
