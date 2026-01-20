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

namespace BiblePathsCore.Pages.PBE
{
    [Authorize]
    public class AddQuizModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public AddQuizModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public QuizGroupStat Quiz { get; set; }

        [BindProperty] 
        public String BibleId { get; set; }

        [BindProperty]
        public bool TeamsAvailable { get; set; } = false;
        public QuizUser PBEUser { get; set; }

        public async Task<IActionResult> OnGetAsync(string BibleId)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (!PBEUser.IsValidPBEQuizHost()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a PBE Quiz" }); }
          
            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            //Initialize Select Lists
            ViewData["BookSelectList"] = await BibleBook.GetBookAndBookListSelectListAsync(_context, BibleId);
            ViewData["TemplateSelectList"] = await PredefinedQuiz.GetTemplateSelectListAsync(_context, PBEUser);
            var MyTeams = await QuizTeam.GetMyTeamsSelectListAsync(_context, PBEUser);
            TeamsAvailable = MyTeams.Count > 1; // there's always the default "<Select a Team>" entry
            ViewData["TeamSelectList"] = MyTeams;
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            // confirm our user is a valid PBE User. 
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);
            if (!PBEUser.IsValidPBEQuizHost()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a PBE Quiz" }); }

            if (Quiz.BookNumber == 0 && Quiz.PredefinedQuiz == 0)
            {
                ModelState.AddModelError("Quiz.BookNumber", "You must select either a Book/BookList or a Template.");
                ModelState.AddModelError("Quiz.PredefinedQuiz", "You must select either a Book/BookList or a Template.");
            }
            if (!ModelState.IsValid)
            {
                //Initialize Select Lists
                ViewData["BookSelectList"] = await BibleBook.GetBookAndBookListSelectListAsync(_context, BibleId);
                ViewData["TemplateSelectList"] = await PredefinedQuiz.GetTemplateSelectListAsync(_context, PBEUser);
                var MyTeams = await QuizTeam.GetMyTeamsSelectListAsync(_context, PBEUser);
                TeamsAvailable = MyTeams.Count > 1; // there's always the default "<Select a Team>" entry
                ViewData["TeamSelectList"] = MyTeams;
                return Page();
            }

            // Now let's create an empty Quiz
            var emptyQuiz = new QuizGroupStat
            {
                Created = DateTime.Now,
                Modified = DateTime.Now,
                QuizUser = PBEUser,
                IsDeleted = false
            };

            if (await TryUpdateModelAsync<QuizGroupStat>(
                emptyQuiz,
                "Quiz",   // Prefix for form value.
                q => q.GroupName, q => q.BookNumber, q => q.PredefinedQuiz, q => q.QuizTeamId))
            {
                if (emptyQuiz.PredefinedQuiz > 0)
                {
                    // Template trumps Book/BookList so we set BookNumber 0
                    emptyQuiz.BookNumber = 0;
                }
                _context.QuizGroupStats.Add(emptyQuiz);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("/PBE/Quiz", new { BibleId = this.BibleId, QuizID = emptyQuiz.Id, Message = String.Format("Quiz {0} successfully created...", emptyQuiz.GroupName) });
        }
    }
}
