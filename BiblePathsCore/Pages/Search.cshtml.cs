using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BiblePathsCore
{
    public class SearchModel : PageModel
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public SearchModel(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        public IList<BibleVerse> BibleVerses { get;set; }
        public bool NoResults { get; set; }

        [BindProperty(SupportsGet = true)]
        public string BibleId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string PageSearchString { get; set; }
        public List<SelectListItem> BibleSelectList { get; set; }

        public async Task<IActionResult> OnGetAsync(string SearchString, string BibleId)
        {
            if (this.BibleId != null) { BibleId = this.BibleId; }
            BibleId = await Bible.GetValidBibleIdAsync(_context, BibleId);
            this.BibleId = BibleId;

            BibleSelectList = await GetBibleSelectListAsync();

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

        private async Task<List<SelectListItem>> GetBibleSelectListAsync()
        {
            return await _context.Bibles.Select(b =>
                              new SelectListItem
                              {
                                  Value = b.Id,
                                  Text = b.Language + "-" + b.Version
                              }).ToListAsync();
        }
    }
}
