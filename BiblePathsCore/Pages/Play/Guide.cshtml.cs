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
using Microsoft.AspNetCore.SignalR;

namespace BiblePathsCore.Pages.Play
{
    public class GuideModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;
        private readonly IHubContext<Hubs.GameTeamHub> _hubContext;

        public GuideModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context, IHubContext<Hubs.GameTeamHub> hubContext)
        {
            _userManager = userManager;
            _context = context;
            _hubContext = hubContext;
        }
        public GameGroup Group { get; set; }
        public PathNode CurrentStep { get; set; }
        public Path Path { get; set; }
        
        [BindProperty]
        public GameTeam Team { get; set; }
        public string UserMessage { get; set; }

        //[PageRemote(
        //    ErrorMessage = "Plese select a word that is NOT found in the text below",
        //    AdditionalFields = "__RequestVerificationToken",
        //    HttpMethod = "post",
        //    PageHandler = "CheckGuideWord"
        //)]
        //[BindProperty]
        //public string UserGuideWord { get; set; }

        public async Task<IActionResult> OnGetAsync(int GroupId, int TeamId, string Message)
        {
            Group = await _context.GameGroups.FindAsync(GroupId);
            if (Group == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find that Group" }); }
            Team = await _context.GameTeams.FindAsync(TeamId);
            if (Team == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We were not able to find that Team" }); }
            if (Team.GroupId != Group.Id) { return RedirectToPage("/error", new { errorMessage = "That's Odd! The Team and Group do not match" }); }
            string BibleId = await GameTeam.GetValidBibleIdAsync(_context, null);

            Path = await _context.Paths.FindAsync(Group.PathId);
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Very Odd! We were not able to find the Path for this Group" }); }

            CurrentStep = await _context.PathNodes.FindAsync(Team.CurrentStepId);

            _ = await CurrentStep.AddGenericStepPropertiesAsync(_context, BibleId);
            _ = await CurrentStep.AddPathStepPropertiesAsync(_context);
            CurrentStep.Verses = await CurrentStep.GetBibleVersesAsync(_context, BibleId, true, false);

            ViewData["KeyWordSelectList"] = await Team.GetKeyWordSelectListAsync(_context, CurrentStep);
            if (Team.BoardState == (int)GameTeam.GameBoardState.WordSelectOffPath)
            {
                Message = "Your Team seems to have fallen off the path, try to get them back on";
            }

            UserMessage = GetUserMessage(Message);
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return RedirectToPage("Guide", new
                {
                    GroupId = Team.GroupId,
                    TeamId = Team.Id
                });
            }

            // Now let's go grab our Team
            GameTeam UpdateTeam = await _context.GameTeams.FindAsync(Team.Id);
            if (UpdateTeam == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We were not able to find that Team" }); }

            if (await TryUpdateModelAsync<GameTeam>(
                UpdateTeam,
                "Team",   // Prefix for form value.
                t => t.KeyWord, t => t.GuideWord))
            {
                UpdateTeam.BoardState = (int)GameTeam.GameBoardState.StepSelect;
                await _context.SaveChangesAsync();
                // Let's signal the StateChange to Clients using SignalR
                string GroupName = "\"" + Team.Id.ToString() + "\"";
                await _hubContext.Clients.Group(GroupName).SendAsync("StateChange");

            }

            return RedirectToPage("Guide", new
            {
                GroupId = UpdateTeam.GroupId,
                TeamId = UpdateTeam.Id
            });
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
        //public async Task<JsonResult> OnPostCheckGuideWordAsync()
        //{
        //    if (await Path.PathNameAlreadyExistsStaticAsync(_context, Name))
        //    {
        //        return new JsonResult("Sorry, this Name is already in use.");
        //    }
        //    return new JsonResult(true);
        //}

    }
}
