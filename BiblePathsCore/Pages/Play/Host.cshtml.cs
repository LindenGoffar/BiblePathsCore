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

namespace BiblePathsCore.Pages.Play
{
    public class HostModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public HostModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        public GameGroup Group { get; set; }

        public PathNode CurrentStep { get; set; }
        public Path Path { get; set; }

        [BindProperty]
        public GameTeam Team { get; set; }
        public async Task<IActionResult> OnGetAsync(int GroupId, int TeamId)
        {
            int StepId = 0;
            Group = await _context.GameGroups.FindAsync(GroupId);
            if (Group == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find that Group" }); }
            Team = await _context.GameTeams.FindAsync(TeamId);
            if (Team == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We were not able to find that Team" }); }
            if (Team.GroupId != Group.Id) { return RedirectToPage("/error", new { errorMessage = "That's Odd! The Team and Group do not match" }); }

            Path = await _context.Paths.FindAsync(Group.PathId);
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Very Odd! We were not able to find the Path for this Group" }); }
            // We likely need that first step. 
            _ = await Path.AddCalculatedPropertiesAsync(_context);
            
            if (Team.CurrentStepId == 0)
            {
                StepId = Path.FirstStepId;
            }
            else
            {
                StepId = Team.CurrentStepId;
            }

            CurrentStep = await _context.PathNodes.FindAsync(StepId);
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        //public async Task<IActionResult> OnPostAsync()
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        ViewData["PathSelectList"] = await GameGroup.GetPathSelectListAsync(_context);
        //        return Page();
        //    }

        //    // Now let's create an empty group
        //    var emptyGroup = new GameGroup
        //    {
        //        Created = DateTime.Now,
        //        Modified = DateTime.Now,
        //        GroupState = (int)GameGroup.GameGroupState.Open,
        //        Owner = user.Email
        //    };

        //    if (await TryUpdateModelAsync<GameGroup>(
        //        emptyGroup,
        //        "Group",   // Prefix for form value.
        //        g => g.Name, g => g.PathId))
        //    {
        //        _context.GameGroups.Add(emptyGroup);
        //        await _context.SaveChangesAsync();

        //        // Now we need to parse our Teams and add/remove
        //        foreach (GameTeam Team in Teams)
        //        {
        //            if (Team.Name != null)
        //            {
        //                if (Team.Name.Length > 0)
        //                {
        //                    var emptyTeam = new GameTeam
        //                    {
        //                        CurrentStepId = 0,
        //                        TeamType = 0,
        //                        BoardState = (int)GameTeam.GameBoardState.Initialize,
        //                        Created = DateTime.Now,
        //                        Modified = DateTime.Now,
        //                    };
        //                    emptyTeam.Name = Team.Name;
        //                    emptyTeam.Group = emptyGroup;
        //                    _context.GameTeams.Add(emptyTeam);
        //                }
        //            }
        //        }
        //        await _context.SaveChangesAsync();
        //        return RedirectToPage("Group", new { Id = emptyGroup.Id, Message = String.Format("Group {0} successfully created...", emptyGroup.Name) });
        //    }

        //    return Page();
        //}

    }
}
