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
    public class SearchModel : PageModel
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public SearchModel(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        public IList<BibleVerses> BibleVerses { get;set; }
        public bool NoResults { get; set; }
        public string BibleId { get; set;  }

        [BindProperty(SupportsGet = true)]
        public string PageSearchString { get; set; }

        public async Task<IActionResult> OnGetAsync(string SearchString)
        {
            BibleId = await Bibles.GetValidBibleIdAsync(_context, BibleId);
            NoResults = false; 
            if(PageSearchString != null) { SearchString = PageSearchString; }
            if(SearchString != null ) { PageSearchString = SearchString; }

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
