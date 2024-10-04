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
    public class DeleteCommentaryModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public DeleteCommentaryModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public CommentaryBook Commentary { get; set; }
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

            Commentary = await _context.CommentaryBooks.FindAsync(id);
            if (Commentary == null) 
            { 
                return RedirectToPage("/error", new
                {
                    errorMessage = "That's Odd! We were unable to retrieve this Commentary... Maybe try that again?"
                });
            }

            // confirm user is a Moderator
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);
            if (!PBEUser.IsModerator) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have permissions to delete this Commentary entry." }); }

            // Now let's go ahead and remove this entry... checks has passed. 
             _context.CommentaryBooks.Remove(Commentary);

            await _context.SaveChangesAsync();

            return RedirectToPage("./Commentaries");
        }
    }
}
