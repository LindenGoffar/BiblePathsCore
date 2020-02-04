using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB
{
    public partial class QuizBookLists
    {
        public QuizBookLists()
        {
            QuizBookListBookMap = new HashSet<QuizBookListBookMap>();
        }

        public int Id { get; set; }
        public string BookListName { get; set; }
        public DateTimeOffset? Created { get; set; }
        public DateTimeOffset? Modified { get; set; }
        public bool IsDeleted { get; set; }

        public virtual ICollection<QuizBookListBookMap> QuizBookListBookMap { get; set; }
    }
}
