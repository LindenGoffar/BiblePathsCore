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
    public class DeleteQuizModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public DeleteQuizModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public QuizGroupStat Quiz { get; set; }
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
                Quiz = await _context.QuizGroupStats.Where(Q => Q.Id == id).SingleAsync();
            }
            catch {
                return RedirectToPage("/error", new
                {
                    errorMessage = "That's Odd! We were unable to retrieve this Quiz... Maybe try that again?"
                });
            }

            // confirm Quiz Owner
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);
            if (Quiz.QuizUser != PBEUser) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Quiz Owner may delete a Quiz" }); }

            // Let's track this event 
            // _ = await Path.RegisterEventAsync(_context, EventType.PathDeleted, Path.Id.ToString());

            // Then we set the Quiz to isDeleted, we want to keep these around for refrence to QuizQuestion Stats.
            if (Quiz != null)
            {
                _context.Attach(Quiz).State = EntityState.Modified;
                Quiz.Modified = DateTime.Now;
                Quiz.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Quizzes");
        }
    }
}
