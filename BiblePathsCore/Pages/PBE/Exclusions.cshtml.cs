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
    public class ExclusionsModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public ExclusionsModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<PBEExclusion> Exclusions { get;set; }
        public QuizUser PBEUser { get; set; }
        public string BibleId { get; set; }
        public string UserMessage { get; set;  }

        public async Task<IActionResult> OnGetAsync(string BibleId, string Message)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (!PBEUser.IsQuizModerator()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to manage PBE Exclusions" }); }

            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            

            List<QuizQuestion> ExclusionQuestions = await _context.QuizQuestions.Where(E => E.Type == (int)QuestionType.Exclusion
                                                                                        && E.IsDeleted == false)
                                                                                .ToListAsync();
            Exclusions = new List<PBEExclusion>();

            foreach (var exclusionQuestion in ExclusionQuestions)
            {
                // This could get a little expensive as we are looking up Bookname for each however
                // the number of overall exclusions should be limited. 
                PBEExclusion exclusion = new PBEExclusion(exclusionQuestion);
                _ = await exclusion.PopulateExclusionAsync(_context);
                Exclusions.Add(exclusion);
            }

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
