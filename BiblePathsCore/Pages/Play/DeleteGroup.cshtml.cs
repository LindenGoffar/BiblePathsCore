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
    public class DeleteGroupModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public DeleteGroupModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
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

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                Group = await _context.GameGroups.Include(G => G.GameTeams)
                                                 .Where(G => G.Id == id).SingleAsync();
            }
            catch {
                return RedirectToPage("/error", new
                {
                    errorMessage = "That's Odd! We were unable to retrieve this Group... Not sure what to tell you!"
                });
            }

            // confirm Group Owner
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (Group.Owner != user.Email) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Group Owner may delete a Group" }); }

            // First we need to iterate through each Step and delete them one by one, steps are a leaf node so this should be OK.
            foreach (GameTeam team in Group.GameTeams)
            {
                _context.GameTeams.Remove(team);
            }
            _context.GameGroups.Remove(Group);
            await _context.SaveChangesAsync();
            return RedirectToPage("./MyGroups");
        }
    }
}
