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
    public class ChallengedQuestionsModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public ChallengedQuestionsModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IList<QuizQuestion> Questions { get;set; }
        public QuizUser PBEUser { get; set; }
        public string BibleId { get; set; }

        public async Task<IActionResult> OnGetAsync(string BibleId)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            this.BibleId = await QuizQuestion.GetValidBibleIdAsync(_context, BibleId);

            Questions = await _context.QuizQuestions.Where(Q => (Q.BibleId == this.BibleId || Q.BibleId == null) && Q.Challenged == true && Q.IsDeleted == false)
                                        .Include(Q => Q.QuizAnswers)
                                        .OrderBy(Q => Q.BookNumber)
                                        .ThenBy(Q => Q.Chapter)
                                        .ThenBy(Q => Q.EndVerse)
                                        .ToListAsync();

            foreach (QuizQuestion Question in Questions)
            {
                _ = await Question.PopulatePBEQuestionAndBookInfoAsync(_context);
                Question.CheckUserCanEdit(PBEUser);
            }      

            return Page();
        }
    }
}
