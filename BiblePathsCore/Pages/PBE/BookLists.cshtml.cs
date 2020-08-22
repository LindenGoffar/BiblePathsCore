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

namespace BiblePathsCore.Pages.PBE
{
    [Authorize]
    public class BookListsModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public BookListsModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<QuizBookLists> BookLists { get;set; }
        public QuizUsers PBEUser { get; set; }
        public string BibleId { get; set; }
        public string UserMessage { get; set;  }

        public async Task<IActionResult> OnGetAsync(string BibleId, string Message)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUsers.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            this.BibleId = await Bibles.GetValidPBEBibleIdAsync(_context, BibleId);

            BookLists = await _context.QuizBookLists.Include(L => L.QuizBookListBookMap)
                                                    .Where(L => L.IsDeleted == false)
                                                    .ToListAsync();

            foreach (QuizBookLists BookList in BookLists)
            {
                foreach(QuizBookListBookMap BookMap in BookList.QuizBookListBookMap)
                {
                    _ = await BookMap.AddBookNameAsync(_context, this.BibleId);
                }
            }
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
