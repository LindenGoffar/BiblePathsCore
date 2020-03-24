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
    public class IndexModel : PageModel
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public IndexModel(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        public IList<Paths> Paths { get;set; }

        public async Task OnGetAsync()
        {
            Paths = await _context.Paths.Where(P => P.IsDeleted == false && P.IsPublished == true).OrderBy(P => P.ComputedRating).ToListAsync();
        }
    }
}
