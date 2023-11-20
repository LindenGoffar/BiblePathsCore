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
    public class ChallengeQuestionModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public ChallengeQuestionModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public QuizUser PBEUser { get; set; }

        public void OnGet()
        {
            RedirectToPage("/error", new { errorMessage = "That's Odd! This page should never be hit... " });

        }

        public async Task<IActionResult> OnPostAsync(int? id, string ChallengeComment, string ReturnPath, int ReturnQuizID = 0)
        {
            if (id == null)
            {
                return NotFound();
            }

            QuizQuestion QuestionToUpdate = await _context.QuizQuestions.FindAsync((int)id.Value);
            if (QuestionToUpdate == null) {
                return RedirectToPage("/error", new
                {
                    errorMessage = "That's Odd! We were unable to retrieve this Question... Maybe go back and try that again?"
                });
            }

            // confirm our user is a valid PBE User. 
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);
            if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to challenge a PBE question" }); }

            if (QuestionToUpdate != null)
            {
                _context.Attach(QuestionToUpdate).State = EntityState.Modified;
                QuestionToUpdate.Modified = DateTime.Now;
                QuestionToUpdate.Challenged = true;
                QuestionToUpdate.ChallengedBy = PBEUser.Email;
                QuestionToUpdate.ChallengeComment = ChallengeComment;
                await _context.SaveChangesAsync();
            }
            switch (ReturnPath)
            {
                case "Questions":
                    return RedirectToPage("./Questions", new { BibleId = QuestionToUpdate.BibleId, BookNumber = QuestionToUpdate.BookNumber, Chapter = QuestionToUpdate.Chapter });
                // break; not needed unreachable

                case "Quiz":
                    return RedirectToPage("./Quiz", new { BibleId = QuestionToUpdate.BibleId, QuizId = ReturnQuizID, Message = "Previous Question Challenged!"});
                // break; not needed unreachable

                default:
                    return RedirectToPage("./Questions", new { BibleId = QuestionToUpdate.BibleId, BookNumber = QuestionToUpdate.BookNumber, Chapter = QuestionToUpdate.Chapter });
                    // break; not needed unreachable
            }
        }
    }
}
