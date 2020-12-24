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
    [Authorize]
    public class AddGroupModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public AddGroupModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public GameGroup Group { get; set; }

        [BindProperty]
        public List<GameTeam> Teams { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            // IdentityUser user = await _userManager.GetUserAsync(User);

            ViewData["PathSelectList"] = await GameGroup.GetPathSelectListAsync(_context);
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            IdentityUser user = await _userManager.GetUserAsync(User);

            if (!ModelState.IsValid)
            {
                ViewData["PathSelectList"] = await GameGroup.GetPathSelectListAsync(_context);
                return Page();
            }

            // Now let's create an empty group
            var emptyGroup = new GameGroup
            {
                Created = DateTime.Now,
                Modified = DateTime.Now,
                GroupState = (int)GameGroup.GameGroupState.Open,
                Owner = user.Email
            };

            if (await TryUpdateModelAsync<GameGroup>(
                emptyGroup,
                "Group",   // Prefix for form value.
                g => g.Name, g => g.PathId))
            {
                _context.GameGroups.Add(emptyGroup);
                await _context.SaveChangesAsync();

                // go get the Path so we can set first step
                Path path = await _context.Paths.FindAsync(emptyGroup.PathId);
                if (path == null) { return RedirectToPage("/error", new { errorMessage = "That's Very Odd! We were not able to find the Path for this Group" }); }
                // We will need that first step. 
                _ = await path.AddCalculatedPropertiesAsync(_context);
                // Now we need to parse our Teams and add/remove
                foreach (GameTeam Team in Teams)
                {
                    if (Team.Name != null)
                    {
                        if (Team.Name.Length > 0)
                        {
                            var emptyTeam = new GameTeam
                            {
                                CurrentStepId = path.FirstStepId,
                                TeamType = 0,
                                BoardState = (int)GameTeam.GameBoardState.WordSelect,
                                StepNumber = 1, 
                                Created = DateTime.Now,
                                Modified = DateTime.Now,
                            };
                            emptyTeam.Name = Team.Name;
                            emptyTeam.Group = emptyGroup;
                            _context.GameTeams.Add(emptyTeam);
                        }
                    }
                }
                await _context.SaveChangesAsync();
                return RedirectToPage("Group", new { Id = emptyGroup.Id, Message = String.Format("Group {0} successfully created...", emptyGroup.Name) });
            }

            return Page();
        }

    }
}
