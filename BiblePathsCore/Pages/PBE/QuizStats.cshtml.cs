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
    public class QuizStatsModel : PageModel
    {

        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public QuizStatsModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public QuizGroupStat Quiz { get; set; }
        public string BibleId { get; set; }
        public bool ShowTeamInfo { get; set; } = false;


        public async Task<IActionResult> OnGetAsync(string BibleId, int QuizId)
        {
            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            // Let's grab the Quiz Object with Stats 
            Quiz = await _context.QuizGroupStats.FindAsync(QuizId);
            if (Quiz == null)
            {
                return RedirectToPage("/error", new { errorMessage = "That's Odd... We were unable to find this Quiz" });
            }

            IdentityUser user = await _userManager.GetUserAsync(User);
            if (user != null)
            { 
                // User is Auth'd let's go see if they are a Team Coach, if so we'll show Team Details.
                QuizUser PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);
                QuizTeam Team = await QuizTeam.GetTeamByIdAsync(_context, Quiz.QuizTeamId);
                if (Team.IsThisMyTeam(_context, PBEUser)) { ShowTeamInfo = true; } // Only Show Team Info if we have an Authenticated Coach
            }

            // Populate Basic Quiz Info 
            _ = await Quiz.AddQuizPropertiesAsync(_context, BibleId);
            _ = await Quiz.AddDetailedQuizStatsAsync(_context, BibleId);

            return Page();
        }
    }
}
