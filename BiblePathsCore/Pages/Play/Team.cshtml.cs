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
using System.Collections;
using Microsoft.AspNetCore.SignalR;

namespace BiblePathsCore.Pages.Play
{
    public class TeamModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;
        private readonly IHubContext<Hubs.GameTeamHub> _hubContext;

        public TeamModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context, IHubContext<Hubs.GameTeamHub> hubContext)
        {
            _userManager = userManager;
            _context = context;
            _hubContext = hubContext;
        }
        public GameGroup Group { get; set; }
        public GameTeam Team { get; set;  }
        //public List<PathNode> Steps { get; set; }
        public Path Path { get; set; }
        public string UserMessage { get; set; }
        public async Task<IActionResult> OnGetAsync(int GroupId, int TeamId, string Message)
        {
            try
            {
                Group = await _context.GameGroups.Include(g => g.GameTeams).Where(g => g.Id == GroupId).SingleAsync();
            }
            catch
            {
                return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find that Group" });
            }
            try { Team = Group.GameTeams.Single(t => t.Id == TeamId); }
            catch { return RedirectToPage("/error", new { errorMessage = "That's Odd! We were not able to find that Team" }); }

            string BibleId = await GameTeam.GetValidBibleIdAsync(_context, null);

            Path = await _context.Paths.FindAsync(Group.PathId);
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Very Odd! We were not able to find the Path for this Group" }); }

            // This has bee moved to an View Component, as an attempt to address perf issues... no luck. 
            //_ = await Path.AddCalculatedPropertiesAsync(_context);

            //if (Team.BoardState == (int)GameTeam.GameBoardState.StepSelect)
            //{
            //    Steps = await Team.GetTeamStepsAsync(_context, BibleId);
            //}
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
