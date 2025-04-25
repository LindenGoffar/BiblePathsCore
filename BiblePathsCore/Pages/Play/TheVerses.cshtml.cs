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
    public class TheVersesModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public TheVersesModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public GameGroup Group { get; set; }
        public List<BibleVerse> Verses { get; set; }
        public string TheWord { get; set; }
        public string BibleId { get; set; }
        public string UserMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int Id, string TheWord, string BibleId, string Message)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);

            Group = await _context.GameGroups.FindAsync(Id);
            if (Group == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find that Game" }); }
            if (Group.Owner != user.Email) { return RedirectToPage("/error", new { errorMessage = "Sorry, Only the owner can host a Game" }); }
            _context.Entry(Group)
            .Collection(g => g.GameTeams)
            .Load();

            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            Verses = await WordCount.GetVerseListForWordFromBookOrBookListAsync(_context, TheWord, BibleId, Group.BookNumber);

            this.TheWord = TheWord;
            UserMessage = GetUserMessage(Message);
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
