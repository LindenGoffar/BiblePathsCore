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

namespace BiblePathsCore.Pages.Play
{
    public class CheckResponseModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public CheckResponseModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        public GameGroup Group { get; set; }
        public GameTeam Team { get; set;  }
        public Path Path { get; set; }

        public async Task<IActionResult> OnGetAsync(int GroupId, int TeamId, int StepId)
        {
            Group = await _context.GameGroups.FindAsync(GroupId);
            if (Group == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find that Group" }); }
            Team = await _context.GameTeams.FindAsync(TeamId);
            if (Team == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We were not able to find that Team" }); }
            if (Team.GroupId != Group.Id) { return RedirectToPage("/error", new { errorMessage = "That's Odd! The Team and Group do not match" }); }
            string BibleId = await GameTeam.GetValidBibleIdAsync(_context, null);
            
            // Now let's check that StepId 
            if (StepId == Team.CurrentStepId)
            {
                PathNode CurrentStep = await _context.PathNodes.FindAsync(Team.CurrentStepId);
                if (CurrentStep == null)
                {
                    return RedirectToPage("/error", new { errorMessage = "That's Very Odd! We couldn't find Current Step" });
                }
                _ = await CurrentStep.AddGenericStepPropertiesAsync(_context, BibleId);
                _ = await CurrentStep.AddPathStepPropertiesAsync(_context);

                // We have a winner! Let's update the Team Object
                _context.Attach(Team);
                Team.Modified = DateTime.Now;
                if (CurrentStep.FWStepId > 0)
                {
                    Team.CurrentStepId = CurrentStep.FWStepId;
                    Team.BoardState = (int)GameTeam.GameBoardState.WordSelect;
                }
                else
                {
                    Team.BoardState = (int)GameTeam.GameBoardState.Completed;
                }
                await _context.SaveChangesAsync();

                return RedirectToPage("Team", new { GroupId = Team.GroupId, TeamId = Team.Id, Message = "Good Job! You're on the right Path" });
            }
            else
            {
                _context.Attach(Team);
                Team.Modified = DateTime.Now;
                Team.BoardState = (int)GameTeam.GameBoardState.WordSelect;
                return RedirectToPage("Team", new { GroupId = Team.GroupId, TeamId = Team.Id, Message = "Uh Oh! You've drifted off the Path" });
            }
        }
    }
}
