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
        public PathNode Step { get; set; }
        public Path Path { get; set; }
        public void OnGet(int? id)
        {
            RedirectToPage("/error", new { errorMessage = "That's Odd! The Delete page should never be hit... " });
        }

        public async Task<IActionResult> OnPostAsync(int? id, int pathId, int experienceID = 0)
        {
            if (id == null)
            {
                return NotFound();
            }

            Step = await _context.PathNodes.FindAsync(id);
            Path = await _context.Paths.FindAsync(pathId);

            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Path" }); }
            if (Step == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Step" }); }
            if (Step.PathId != Path.Id) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We've got an Path/Step mismatch" }); }

            IdentityUser user = await _userManager.GetUserAsync(User);
            if (!Path.IsPathOwner(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Path Owner may delete a Step" }); }
            
            // This is a bit convoluted, but we want to grab the position of the previous step unless it's the last step in the path. 
            int PreviousStepPosition = Step.Position - 10 >= 10 ? Step.Position - 10 : 0;

            // Now we do the actual delete. 
            _context.PathNodes.Remove(Step);
            await _context.SaveChangesAsync();

            // We need to re-position each node in the path to ensure safe ordering
            _ = await Path.RedistributeStepsAsync(_context);

            // We also need to update the Path Object. 
            _context.Attach(Path);
            Path.Modified = DateTime.Now;

            if (Path.Type == (int)PathType.Commented)
            {
                if (experienceID == 1)
                {
                    return RedirectToPage("/CommentedPaths/Builder", new { PathId = Path.Id, StepPosition = PreviousStepPosition});
                }
                else
                {
                    return RedirectToPage("/CommentedPaths/Steps", new { PathId = Path.Id });
                }
            }
            else
            {
                return RedirectToPage("/Paths/Steps", new { PathId = Path.Id });
            }
        }
    }
}
