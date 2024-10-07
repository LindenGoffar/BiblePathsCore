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
    public class DeleteQuestionModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public DeleteQuestionModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public QuizQuestion Question { get; set; }
        public QuizUser PBEUser { get; set; }

        public void OnGet()
        {
            RedirectToPage("/error", new { errorMessage = "That's Odd! This page should never be hit... " });

        }

        public async Task<IActionResult> OnPostAsync(int? id, string ReturnPath)
        {
            if (id == null)
            {
                return NotFound();
            }

            Question = await _context.QuizQuestions.FindAsync((int)id.Value);
            if (Question == null) {
                return RedirectToPage("/error", new
                {
                    errorMessage = "That's Odd! We were unable to retrieve this Question... Maybe try that again?"
                });
            }

            // confirm our user is a valid PBE User. 
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);
            if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to edit a PBE question" }); }

            // Questions and Answers are not actually deleted, because they are used to calculate Stats so we just set to IsDeleted = true. 
            if (Question != null)
            {
                _context.Attach(Question).State = EntityState.Modified;
                Question.Modified = DateTime.Now;
                Question.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            switch (ReturnPath)
            {
                case "Questions":
                    return RedirectToPage("./Questions", new { BibleId = Question.BibleId, BookNumber = Question.BookNumber, Chapter = Question.Chapter });
                // break; not needed unreachable

                case "ChallengedQuestions":
                    return RedirectToPage("ChallengedQuestions", new { BibleId = Question.BibleId, BookNumber = Question.BookNumber, Chapter = Question.Chapter });
                // break; not needed unreachable

                default:
                    return RedirectToPage("./Questions", new { BibleId = Question.BibleId, BookNumber = Question.BookNumber, Chapter = Question.Chapter });
                    // break; not needed unreachable
            }
        }
    }
}
