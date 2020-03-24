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

namespace BiblePathsCore
{
    public class DeleteStepModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public DeleteStepModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public PathNodes PathNodes { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            return RedirectToPage("/error", new { errorMessage = "That's Odd! This page should never be hit... " });
            //if (id == null)
            //{
            //    return NotFound();
            //}

            //PathNodes = await _context.PathNodes
            //    .Include(p => p.Path).FirstOrDefaultAsync(m => m.Id == id);

            //if (PathNodes == null)
            //{
            //    return NotFound();
            //}
            //return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id, int pathId)
        {
            if (id == null)
            {
                return NotFound();
            }

            PathNodes = await _context.PathNodes.FindAsync(id);

            if (PathNodes != null)
            {
                _context.PathNodes.Remove(PathNodes);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("/Paths/Steps", new { PathId = pathId });
        }
    }
}
