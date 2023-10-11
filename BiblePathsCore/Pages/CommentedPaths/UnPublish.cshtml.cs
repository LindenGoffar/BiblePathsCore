using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BiblePathsCore.Models.DB;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace BiblePathsCore
{
    public class UnPublishCPModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public UnPublishCPModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public Path Path { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                RedirectToPage("/error", new { errorMessage = "That's Odd! The Unpublish action requires a valid Path ID..." });
            }

            Path = await _context.Paths.FindAsync(id);
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We were unable to find this Path." }); }

            // confirm our owner is a valid path owner.
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (!Path.IsPathOwner(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Path Owner is allowed to UnPublish a Path" }); }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return RedirectToPage("/error", new { errorMessage = "That's Odd! Model State is not valid, I can't explain it either... " });
            }

            var pathToUpdate = await _context.Paths.FindAsync(id);

            if (pathToUpdate == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We were unable to find this Path." }); }

            // confirm our owner is a valid path owner.
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (!pathToUpdate.IsPathOwner(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Path Owner is allowed to UnPublish a Path" }); }

            _context.Attach(pathToUpdate).State = EntityState.Modified;
            pathToUpdate.Modified = DateTime.Now;
            pathToUpdate.IsPublished = false;
            await _context.SaveChangesAsync();

            return RedirectToPage("./MyCommentedPaths");
        }
    }
}