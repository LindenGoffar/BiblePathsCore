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
using Microsoft.AspNetCore.Authorization;

namespace BiblePathsCore
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public DeleteModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public Path Path { get; set; }

        public void OnGet(int? id)
        {
            RedirectToPage("/error", new { errorMessage = "That's Odd! This page should never be hit... " });
            //if (id == null)
            //{
            //    return NotFound();
            //}

            //Path = await _context.Paths.FirstOrDefaultAsync(m => m.Id == id);

            //if (Path == null)
            //{
            //    return NotFound();
            //}
            // return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                Path = await _context.Paths.Include(P => P.PathNodes).Where(P => P.Id == id).SingleAsync();
            }
            catch {
                return RedirectToPage("/error", new
                {
                    errorMessage = "That's Odd! We were unable to retrieve this path... Not sure what to tell you!"
                });
            }

            // confirm Path Owner
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (!Path.IsPathOwner(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Path Owner may delete a Path" }); }

            // First we need to iterate through each Step and delete them one by one, steps are a leaf node so this should be OK.
            foreach (PathNode step in Path.PathNodes)
            {
                _context.PathNodes.Remove(step);
            }
            await _context.SaveChangesAsync();
            // Let's track this event 
            _ = await Path.RegisterEventAsync(_context, EventType.PathDeleted, Path.Id.ToString());

            // Then we set the Path to isDeleted, we also unPublish it in case a filter is incorrectly not checking for deleted.
            if (Path != null)
            {
                _context.Attach(Path).State = EntityState.Modified;
                Path.Modified = DateTime.Now;
                Path.IsPublished = false;
                Path.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./MyPaths");
        }
    }
}
