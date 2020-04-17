using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BiblePathsCore.Models.DB;
using Microsoft.EntityFrameworkCore;

namespace BiblePathsCore
{
    public class PathModel : PageModel
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public PathModel(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        public string Name { get; set; }
        public Paths Path { get; set; }
        public async Task<IActionResult> OnGetAsync(string name)
        {
            try
            {
                Path = await _context.Paths.Where(P => P.Name == name && P.IsPublished == true && P.IsDeleted == false).SingleAsync();
                if (Path == null)
                {
                    return RedirectToPage("/Index");
                }
                return RedirectToPage("/Steps", new { PathId = Path.Id });
            }
            catch
            {
                return RedirectToPage("/Index");
            }
        }
    }
}