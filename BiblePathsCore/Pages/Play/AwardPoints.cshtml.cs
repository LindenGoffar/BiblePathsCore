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

namespace BiblePathsCore.Pages.Play
{
    [Authorize]
    public class AwardPointsModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public AwardPointsModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public GameGroup Group { get; set; }

        public void OnGet(int? id)
        {
            RedirectToPage("/error", new { errorMessage = "That's Odd! This page should never be hit... " });
        }

        public async Task<IActionResult> OnPostAsync(int? GroupId, int TeamId, int Points, string BibleID)
        {
            if (GroupId == null)
            {
                return NotFound();
            }

            try
            {
                Group = await _context.GameGroups.Include(G => G.GameTeams)
                                                 .Where(G => G.Id == GroupId).SingleAsync();
            }
            catch {
                return RedirectToPage("/error", new
                {
                    errorMessage = "That's Odd! We were unable to retrieve this Group... Not sure what to tell you!"
                });
            }

            // confirm Group Owner
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (Group.Owner != user.Email) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Group Owner may award Team Points" }); }
            GameTeam Team = Group.GameTeams.Where(T => T.Id == TeamId).SingleOrDefault();
            if (Team == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd... We couldn't find the selected Team" }); }


            // Award Points 
            Team.StepNumber = Team.StepNumber + Points;
            Team.Modified = DateTime.Now;
            await _context.SaveChangesAsync();
            return RedirectToPage("TheWord", new { Id = GroupId, BibleID = BibleID, Message = $"Points Awarded to: {Team.Name}"});
        }
    }
}
