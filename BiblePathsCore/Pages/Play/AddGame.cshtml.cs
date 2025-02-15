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
    public class AddGameModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public AddGameModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public String BibleId { get; set; }
        [BindProperty]
        public GameGroup Game { get; set; }
        [BindProperty]
        public List<GameTeam> Teams { get; set; }
        public QuizUser PBEUser { get; set; }

        public async Task<IActionResult> OnGetAsync(string BibleId)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (!PBEUser.IsValidPBEQuizHost()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient PBE rights to host a new Game" }); }

            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            // "The Word" Game requires a Booknumber or BookList ID
            ViewData["BookSelectList"] = await BibleBook.GetBookAndBookListSelectListAsync(_context, this.BibleId);
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (!PBEUser.IsValidPBEQuizHost()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient PBE rights to host a new Game" }); }

            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            if (!ModelState.IsValid)
            {
                ViewData["TemplateSelectList"] = await PredefinedQuiz.GetTemplateSelectListAsync(_context, PBEUser);
                return Page();
            }

            // Now let's create an empty game
            var emptyGame = new GameGroup
            {
                Created = DateTime.Now,
                Modified = DateTime.Now,
                GroupState = (int)GameGroup.GameGroupState.Open,
                GroupType = (int)GameGroup.GameGroupType.PBEWords,
                Owner = user.Email
            };

            if (await TryUpdateModelAsync<GameGroup>(
                emptyGame,
                "Game",   // Prefix for form value.
                g => g.Name, g => g.BookNumber)) // PathId is used to reference the Template Used
            {
                _context.GameGroups.Add(emptyGame);
                await _context.SaveChangesAsync();

                // Now we need to parse our Teams and add/remove
                foreach (GameTeam Team in Teams)
                {
                    if (Team.Name != null)
                    {
                        if (Team.Name.Length > 0)
                        {
                            var emptyTeam = new GameTeam
                            {
                                TeamType = (int)GameTeam.GameTeamType.PBEWords,
                                BoardState = (int)GameTeam.GameBoardState.WordSelect,
                                StepNumber = 0, // this is used as Points in PBEWords 
                                Created = DateTime.Now,
                                Modified = DateTime.Now,
                            };
                            emptyTeam.Name = Team.Name;
                            emptyTeam.Group = emptyGame;
                            _context.GameTeams.Add(emptyTeam);
                        }
                    }
                }
                await _context.SaveChangesAsync();
                return RedirectToPage("MyGames");
            }

            return Page();
        }

    }
}
