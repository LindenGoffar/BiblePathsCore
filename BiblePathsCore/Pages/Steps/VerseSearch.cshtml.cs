using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;

namespace BiblePathsCore
{
    public class VerseSearchModel : PageModel
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public VerseSearchModel(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        public IList<BibleVerses> BibleVerses { get;set; }
        public Paths Path { get; set; }
        public bool NoResults { get; set; }
        public string BibleId { get; set;  }
        public int StepPosition { get; set; }
        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        public async Task<IActionResult> OnGetAsync(string BibleId, int PathId, int Position)
        {
            //Does the path exist? if not we've got an error. 
            Path = await _context.Paths.FindAsync(PathId);
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Path" }); }
            BibleId = await Path.GetValidBibleIdAsync(_context, BibleId);
            this.BibleId = BibleId;
            StepPosition = Position;
            NoResults = false; 

            if (SearchString == null)
            {
                return Page();
            }
            else
            {
                BibleVerses = await _context.BibleVerses
                                    .Where(v => v.BibleId == BibleId && v.Text.Contains(SearchString))
                                    .OrderBy(v => v.BookNumber).ThenBy(v => v.Chapter).ThenBy(v => v.Verse)
                                    .ToListAsync();
            }
            if (BibleVerses.Count == 0 ) { NoResults = true; }

            return Page();
        }
    }
}
