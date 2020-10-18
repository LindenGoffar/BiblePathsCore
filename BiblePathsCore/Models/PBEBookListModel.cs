using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class QuizBookLists
    {
        public static async Task<bool> ListNameAlreadyExistsStaticAsync(BiblePathsCoreDbContext context, string CheckName)
        {
            if (await context.QuizBookLists.Where(l => l.BookListName.ToLower() == CheckName.ToLower()).AnyAsync())
            {
                return true;
            }
            return false;
        }

        // We allow up to 10 books per Book List, so for the Edit Scenario we'll pad our BookList object with addtional 
        // Book List Book Maps to allow for editing 
        public void PadBookListBookMapsForEdit()
        {
            int MapCount = QuizBookListBookMap.Count;
            int MapsNeeded = 10 - MapCount;
            if (MapsNeeded > 0)
            {
                for( int i = 0; i < MapsNeeded; i++)
                {
                    Models.DB.QuizBookListBookMap BookMap = new QuizBookListBookMap();
                    BookMap.BookNumber = 0;
                    QuizBookListBookMap.Add(BookMap);
                }
            }
        }
    }
    public partial class QuizBookListBookMap
    {
        [NotMapped]
        public string BookName { get; set; }

        public async Task<bool> AddBookNameAsync(BiblePathsCoreDbContext context, string bibleId)
        {
            // Get BookName 
            BookName = await BibleBooks.GetBookNameAsync(context, bibleId, BookNumber);
            return true;
        }
    }
}
