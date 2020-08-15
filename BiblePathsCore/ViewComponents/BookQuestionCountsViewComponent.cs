using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.ViewComponents
{
    public class BookQuestionCountsViewComponent : ViewComponent
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;
        public BookQuestionCountsViewComponent(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string BibleId)
        {
            BibleId = await QuizQuestions.GetValidBibleIdAsync(_context, BibleId);
            List<BibleBooks> ReturnBooks = await BibleBooks.GetPBEBooksWithQuestionsAsync(_context, BibleId);

            return View(ReturnBooks);
        }
    }
}
