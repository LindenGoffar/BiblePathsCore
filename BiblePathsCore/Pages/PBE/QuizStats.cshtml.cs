using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace BiblePathsCore.Pages.PBE
{
    public class QuizStatsModel : PageModel
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public QuizStatsModel(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        public QuizGroupStats Quiz { get; set; }
        public string BibleId { get; set; }

        public async Task<IActionResult> OnGetAsync(string BibleId, int QuizId)
        {
            this.BibleId = await Bibles.GetValidPBEBibleIdAsync(_context, BibleId);

            // Let's grab the Quiz Object with Stats 
            Quiz = await _context.QuizGroupStats.FindAsync(QuizId);
            if (Quiz == null)
            {
                return RedirectToPage("/error", new { errorMessage = "That's Odd... We were unable to find this Quiz" });
            }

            // Populate Basic Quiz Info 
            _ = await Quiz.AddQuizPropertiesAsync(_context, BibleId);
            _ = await Quiz.AddDetailedQuizStatsAsync(_context, BibleId);

            return Page();
        }
    }
}
