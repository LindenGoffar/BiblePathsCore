using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.ViewComponents
{
    public class RecentBookListsViewComponent : ViewComponent
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;
        public RecentBookListsViewComponent(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string BibleId)
        {
            BibleId = await QuizQuestion.GetValidBibleIdAsync(_context, BibleId);

            List<QuizBookList> BookLists = await _context.QuizBookLists.Include(L => L.QuizBookListBookMaps)
                                        .Where(L => L.IsDeleted == false)
                                        .OrderByDescending(L => L.Created)
                                        .Take(3)
                                        .ToListAsync();

            foreach (QuizBookList BookList in BookLists)
            {
                foreach (QuizBookListBookMap BookMap in BookList.QuizBookListBookMaps)
                {
                    _ = await BookMap.AddBookNameAsync(_context, BibleId);
                }
            }

            return View(BookLists);
        }
    }
}
