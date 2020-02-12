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
    public class SelectChapterModel : PageModel
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public SelectChapterModel(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        public IList<BibleBooks> BibleBooks { get; set; }
        public Bibles Bible { get; set;  }
        public Paths Path { get; set; }

        public async Task OnGetAsync(string BibleId, int PathId)
        {
            BibleId = await GetValidBibleIdAsync(BibleId);
            Bible = await _context.Bibles.FindAsync(BibleId);
            BibleBooks = await _context.BibleBooks
                .Include(B => B.BibleChapters).Where(B => B.BibleId == Bible.Id).ToListAsync();
            Path = await _context.Paths.FindAsync(PathId);
        }


        private async Task<string> GetValidBibleIdAsync(string BibleId)
        {
            if (string.IsNullOrEmpty(BibleId) || !(await _context.Bibles.AnyAsync(B => B.Id == BibleId)))
            {
                BibleId = Bibles.DefaultBibleId;
            }
            return BibleId;
        }
    }
}
