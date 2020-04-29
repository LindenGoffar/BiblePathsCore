using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Http;

namespace BiblePathsCore
{
    public class IndexModel : PageModel
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public IndexModel(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        public SortBy OrderedBy { get; set; }
        public IList<Paths> Paths { get;set; }

        public async Task OnGetAsync(SortBy SortOrder = SortBy.HighestRated)
        {
            var paths = from p in _context.Paths
                         select p;
            if (!string.IsNullOrEmpty(SearchString))
            {
                paths = paths.Where(s => s.Name.Contains(SearchString) || s.Topics.Contains(SearchString) && s.IsPublished == true && s.IsDeleted == false);
            }
            else 
            {
                paths = paths.Where(s => s.IsPublished == true && s.IsDeleted == false);
            }

            switch (SortOrder)
            {
                case SortBy.HighestRated:
                    paths = paths.OrderBy(p => p.ComputedRating);
                    break;

                case SortBy.Newest: 
                    paths = paths.OrderByDescending(p => p.Created);
                    break;

                case SortBy.Shortest:
                    paths = paths.OrderBy(p => p.Length);
                    break;

                case SortBy.Reads:
                    paths = paths.OrderByDescending(p => p.Reads);
                    break;

                default:
                    paths = paths.OrderBy(p => p.ComputedRating);
                    break;
            }

            OrderedBy = SortOrder;

            Paths = await paths.ToListAsync();

            foreach (Paths Path in Paths)
            {
                _ = await Path.AddCalculatedPropertiesAsync(_context);
            }
        }
    }
}
