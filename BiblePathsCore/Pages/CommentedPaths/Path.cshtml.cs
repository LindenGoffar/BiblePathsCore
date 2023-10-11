using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using Microsoft.EntityFrameworkCore;

namespace BiblePathsCore
{
    public class CommentedPathModel : PageModel
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public CommentedPathModel(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        public string Name { get; set; }
        public Path Path { get; set; }
        public async Task<IActionResult> OnGetAsync(string name)
        {
            try
            {
                Path = await _context.Paths.Where(P => P.Name == name 
                                                    && P.Type == (int)PathType.Commented
                                                    && P.IsPublished == true 
                                                    && P.IsDeleted == false)
                                            .SingleAsync();
                if (Path == null)
                {
                    return RedirectToPage("Index");
                }
                // We need to find the first Step
                _ = await Path.AddCalculatedPropertiesAsync(_context);
                return RedirectToPage("/Steps/Step", new { Id = Path.FirstStepId });
            }
            catch
            {
                return RedirectToPage("Index");
            }
        }
    }
}