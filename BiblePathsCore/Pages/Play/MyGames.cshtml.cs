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
    public class MyGamesModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public MyGamesModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public string BibleId { get; set; }

        public List<GameGroup> Games { get;set; }

        public async Task<IActionResult> OnGetAsync(string BibleId)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);

            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            Games = await _context.GameGroups.Include(G => G.GameTeams)
                                                    .Where(G => G.Owner == user.Email
                                                    && G.GroupType == (int)GameGroup.GameGroupType.TheWord)
                                                    .ToListAsync();

            foreach(GameGroup Game in Games)
            {
                _ = await Game.AddBookListAsync(_context, this.BibleId);
                Game.AddGameName();
            }

            return Page();
        }

    }
}
