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
    public class DeleteExclusionModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public DeleteExclusionModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public QuizQuestion Exclusion { get; set; } // Exclusions are stored as QuizQuestion objects with no linked children
        public QuizUser PBEUser { get; set; }

        public void OnGet(int? id)
        {
            RedirectToPage("/error", new { errorMessage = "That's Odd! This page should never be hit... " });
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                Exclusion = await _context.QuizQuestions.FindAsync(id);
            }
            catch {
                return RedirectToPage("/error", new
                {
                    errorMessage = "That's Odd! We were unable to retrieve this Exclusion... Maybe try that again?"
                });
            }

            // confirm permissions to manage PBE Exclusions. 
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);
            if (!PBEUser.IsQuizModerator()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to delete a PBE Exclusion" }); }

            // Since Exclusions are technically questions, we "Soft Delete" them as they could have child objects. 
            if (Exclusion != null)
            {
                _context.Attach(Exclusion).State = EntityState.Modified;
                Exclusion.Modified = DateTime.Now;
                Exclusion.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Exclusions");
        }
    }
}
