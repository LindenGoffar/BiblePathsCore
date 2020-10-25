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

namespace BiblePathsCore.Pages.PBE
{
    [Authorize]
    public class QuizModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public QuizModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public QuizQuestions Question { get; set; }
        public QuizGroupStats Quiz { get; set; }
        public QuizUsers PBEUser { get; set; }
        public string BibleId { get; set; }
        public string UserMessage { get; set;  }

        public async Task<IActionResult> OnGetAsync(string BibleId, int QuizId, string Message)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUsers.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            this.BibleId = await Bibles.GetValidPBEBibleIdAsync(_context, BibleId);

            // Let's grab the Quiz Object
            Quiz = await _context.QuizGroupStats.FindAsync(Quiz);
            if (Quiz == null)
            {
                return RedirectToPage("/error", new { errorMessage = "That's Odd... We were unable to find this Quiz" });
            }
            if (Quiz.QuizUser != PBEUser) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Quiz Owner can run a Quiz" }); }

            Question = await Quiz.GetNextQuizQuestionAsync(_context, BibleId);

            UserMessage = GetUserMessage(Message);
            return Page();
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
