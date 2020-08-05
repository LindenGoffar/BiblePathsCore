using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BiblePathsCore.Pages.Shared
{
    public class SelectChapterModel : PageModel
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public SelectChapterModel(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        public IList<BibleBooks> BibleBooks { get; set; }
        public Bibles Bible { get; set; }
        // public BiblePathsCore.Models.DB.Paths Path { get; set; } // Paths is ambiguous in this namespace
        public int PathId { get; set; }
        public int StepId { get; set; }
        public int StepPosition { get; set; }
        public string TargetPage { get; set; }
        public bool IsPBE { get; set;  }

        public async Task OnGetAsync(string BibleId, int? PathId, int? StepId, int? Position, string TargetPage)
        {
            BibleId = await GetValidBibleIdAsync(BibleId);
            Bible = await _context.Bibles.FindAsync(BibleId);

            // Bible Path/Step specific properties. 
            this.StepId = StepId.HasValue ? StepId.Value : 0;
            StepPosition = Position.HasValue ? Position.Value : 0;
            this.PathId = PathId.HasValue ? PathId.Value : 0;

            this.TargetPage = TargetPage;
            // Let's see if the scnario is PBE?
            if (TargetPage.Contains("PBE"))
            {
                IsPBE = true;
                BibleBooks = await Models.DB.BibleBooks.GetPBEBooksAsync(_context, BibleId);
            }
            else { 
                IsPBE = false;
                BibleBooks = await _context.BibleBooks
                    .Include(B => B.BibleChapters).Where(B => B.BibleId == Bible.Id).ToListAsync();
            }

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