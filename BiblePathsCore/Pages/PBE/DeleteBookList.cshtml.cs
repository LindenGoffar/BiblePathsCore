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
using NuGet.Packaging.Signing;

namespace BiblePathsCore
{
    public class DeleteBookListModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public DeleteBookListModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public QuizBookList BookList { get; set; }
        public QuizUser PBEUser { get; set; }

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
                BookList = await _context.QuizBookLists.Include(B => B.QuizBookListBookMaps).Where(B => B.Id == id).SingleAsync();
            }
            catch {
                return RedirectToPage("/error", new
                {
                    errorMessage = "That's Odd! We were unable to retrieve this BookList... Maybe try that again"
                });
            }

            // confirm Path Owner
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);
            if (!PBEUser.IsQuizModerator()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to delete a PBE BookList" }); }

            // 10/16/2024 Removed Soft Delete... now we just whack em it's more effecient.
 

            // First we need to iterate through each BookList Map and delete them, these are a leaf node so this should be OK.
            foreach (QuizBookListBookMap book in BookList.QuizBookListBookMaps)
            {
                _context.Attach(book);
                _context.QuizBookListBookMaps.Remove(book);
            }
            await _context.SaveChangesAsync();
            // Let's track this event 
            // _ = await Path.RegisterEventAsync(_context, EventType.PathDeleted, Path.Id.ToString());
            //Now let's go ahead and delete this BookMap
            if (BookList != null)
            {
                _context.Attach(BookList).State = EntityState.Modified;
                _context.QuizBookLists.Remove(BookList);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./BookLists");
        }
    }
}
