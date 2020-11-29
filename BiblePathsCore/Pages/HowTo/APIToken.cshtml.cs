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

namespace BiblePathsCore.Pages.HowTo
{
    [Authorize]
    public class APITokenModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public APITokenModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public string TokenString { get; set; }
        public QuizUser PBEUser { get; set; }
        public string UserMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string Message)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (PBEUser.IsQuestionBuilderLocked) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to create an API token" }); }

            TokenString = await PBEUser.GetQuestionAPITokenAsync(_context);

            UserMessage = GetUserMessage(Message);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // confirm our user is a valid PBE User. 
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (PBEUser.IsQuestionBuilderLocked) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to create an API token" }); }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            string NewToken = await PBEUser.CreateQuestionAPITokenAsync(_context);

            return RedirectToPage("APIToken", new {Message = String.Format("Token successfully created: {0}", NewToken) });
        }

        public string GetUserMessage(string Message)
        {
            if (Message != null)
            {
                // Arbitrarily limiting User Message length. 
                if (Message.Length > 0 && Message.Length < 128)
                {
                    return Message;
                }
            }
            return null;
        }
    }
}
