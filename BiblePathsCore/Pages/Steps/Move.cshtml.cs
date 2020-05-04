using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using Microsoft.EntityFrameworkCore;

namespace BiblePathsCore
{
    public class MoveModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public MoveModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public PathNodes Step { get; set; }
        public Paths Path { get; set; }
        public void OnGet()
        {
            RedirectToPage("/error", new { errorMessage = "That's Odd! The Move page should never actually be hit... " });
        }

        public async Task<IActionResult> OnPostAsync(int? id, int spaces)
        {
            if (id == null)
            {
                return RedirectToPage("/error", new { errorMessage = "Sorry! The Step Id cannot be null" });
            }

            Step = await _context.PathNodes.FindAsync(id);
            if (Step == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We were not able to find this Step" }); }
            int StartPosition = Step.Position;
            int TempPosition = StartPosition;

            Path = await _context.Paths.FindAsync(Step.PathId);
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Path" }); }

            IdentityUser user = await _userManager.GetUserAsync(User);
            if (!Path.IsValidPathEditor(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have permissions to move this step." }); }

            if (spaces < 0) // this is the move up scenario
            {
                TempPosition = StartPosition + (10 * spaces) - 4; // the minus 4 pushes us above the target step, where want to be and won't conflict with add steps 5.
                if (TempPosition < 0) { TempPosition = 0; }
            }
            if (spaces > 0) // the move down scenario
            {
                TempPosition = StartPosition + (10 * spaces) + 3; // the plus 3 pushes us above the target step, where want to be and won't conflict with add steps 5.
            }
            // Move the step
            _context.Attach(Step).State = EntityState.Modified;
            Step.Modified = DateTime.Now;
            Step.Position = TempPosition;
            await _context.SaveChangesAsync();

            // Register the event
            if (!Path.IsPathOwner(user.Email))
            {
                _ = await Path.RegisterEventAsync(_context, EventType.NonOwnerEdit, user.Email);
            }
            // Update the Path modified date. 
            _context.Attach(Path);
            Path.Modified = DateTime.Now;
            await _context.SaveChangesAsync();

            // Finally we need to re-position each node in the path to ensure safe ordering
            _ = await Path.RedistributeStepsAsync(_context);

            return RedirectToPage("/Paths/Steps", new { PathId = Step.PathId });
        }
    }
}