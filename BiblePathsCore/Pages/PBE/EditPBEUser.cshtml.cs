using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BiblePathsCore.Pages.PBE
{
    [Authorize]
    public class EditPBEUserModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public EditPBEUserModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public QuizUser EditUser { get; set; }
        public QuizUser PBEUser { get; set; }

        public async Task<IActionResult> OnGetAsync(int Id)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (!PBEUser.IsQuizModerator()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to manage a PBE User" }); }

            EditUser = await _context.QuizUsers.FindAsync(Id);
            if (EditUser == null ) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this User" }); }

            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int Id)
        {
            // confirm our user is a valid PBE User. 
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (!PBEUser.IsQuizModerator()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to manage a PBE User" }); }

            QuizUser UserToUpdate = await _context.QuizUsers.FindAsync(Id);
            if (UserToUpdate == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this User" }); }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (await TryUpdateModelAsync<QuizUser>(
                UserToUpdate,
                 "EditUser",
                 U => U.IsModerator, U => U.IsQuestionBuilderLocked, U => U.IsQuizTakerLocked))
            {
                await _context.SaveChangesAsync();
                return RedirectToPage("./PBEUsers", new { Message = String.Format("PBE User:  {0} successfully updated...", UserToUpdate.Email) } );
            }
            return RedirectToPage("./PBEUsers");
        }
    }
}
