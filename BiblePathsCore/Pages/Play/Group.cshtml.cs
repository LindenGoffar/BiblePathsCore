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
    public class GroupModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public GroupModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public GameGroup Group { get; set; }

        public Path SelectedPath { get; set; }
        public string UserMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int Id, string Message)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);

            Group = await _context.GameGroups.FindAsync(Id);
            if (Group == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find that Group" }); }
            if (Group.Owner != user.Email) { return RedirectToPage("/error", new { errorMessage = "Sorry, Only the owner can manage a Group" }); }
            SelectedPath = await _context.Paths.FindAsync(Group.PathId);
            if (SelectedPath == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find the Path for this Group" }); }

            _context.Entry(Group)
            .Collection(g => g.GameTeams)
            .Load();

            ViewData["PathSelectList"] = await GameGroup.GetPathSelectListAsync(_context);
            UserMessage = GetUserMessage(Message);
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            IdentityUser user = await _userManager.GetUserAsync(User);

            if (!ModelState.IsValid)
            {

                return RedirectToPage("Group", new { Id = Group.Id });
            }

            GameGroup UpdateGroup = await _context.GameGroups.FindAsync(Group.Id);

            if (UpdateGroup == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find that Group" }); }
            if (UpdateGroup.Owner != user.Email) { return RedirectToPage("/error", new { errorMessage = "Sorry, Only the owner can manage a Group" }); }
            _context.Entry(UpdateGroup)
                    .Collection(g => g.GameTeams)
                    .Load();

            if (await TryUpdateModelAsync<GameGroup>(
                UpdateGroup,
                "Group",   // Prefix for form value.
                t => t.PathId))
            {
                UpdateGroup.GroupState = (int)GameGroup.GameGroupState.InPlay;
                UpdateGroup.Modified = DateTime.Now;

                // go get the Path so we can set first step
                Path path = await _context.Paths.FindAsync(UpdateGroup.PathId);
                if (path == null) { return RedirectToPage("/error", new { errorMessage = "That's Very Odd! We were not able to find the Path for this Group" }); }

                // We will need that first step. 
                _ = await path.AddCalculatedPropertiesAsync(_context);

                // Now we need to iterate through each Team and reset it. 

                foreach (GameTeam Team in UpdateGroup.GameTeams)
                {
                    Team.CurrentStepId = path.FirstStepId;
                    Team.TeamType = 0;
                    Team.BoardState = (int)GameTeam.GameBoardState.WordSelect;
                    Team.Modified = DateTime.Now;
                }
                await _context.SaveChangesAsync();
                return RedirectToPage("Group", new { Id = UpdateGroup.Id, Message = String.Format("Group {0} has been Restarted!", UpdateGroup.Name) });
            }
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
