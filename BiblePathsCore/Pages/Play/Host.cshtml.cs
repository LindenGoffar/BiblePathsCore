﻿using System;
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

        //[PageRemote(
        //    ErrorMessage = "Plese select a word that is NOT found in the text below",
        //    AdditionalFields = "__RequestVerificationToken",
        //    HttpMethod = "post",
        //    PageHandler = "CheckGuideWord"
        //)]
        //[BindProperty]
        //public string UserGuideWord { get; set; }

        public async Task<IActionResult> OnGetAsync(int GroupId, int TeamId)
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

            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return RedirectToPage("Host", new
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
            }

            return RedirectToPage("Host", new
            {
                GroupId = UpdateTeam.GroupId,
                TeamId = UpdateTeam.Id
            });
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