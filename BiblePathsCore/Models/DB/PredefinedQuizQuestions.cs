﻿using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class PredefinedQuizQuestions
    {
        public int Id { get; set; }
        public int? PredefinedQuizId { get; set; }
        public int QuestionNumber { get; set; }
        public int BookNumber { get; set; }
        public int Chapter { get; set; }

        public virtual PredefinedQuizzes PredefinedQuiz { get; set; }
    }
}