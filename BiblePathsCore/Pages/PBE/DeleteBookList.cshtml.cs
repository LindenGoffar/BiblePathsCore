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
        public QuizBookLists BookList { get; set; }
        public QuizUsers PBEUser { get; set; }

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
                BookList = await _context.QuizBookLists.Include(B => B.QuizBookListBookMap).Where(B => B.Id == id).SingleAsync();
            }
            catch {
                return RedirectToPage("/error", new
                {
                    errorMessage = "That's Odd! We were unable to retrieve this BookList... Maybe try that again"
                });
            }

            // confirm Path Owner
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUsers.GetOrAddPBEUserAsync(_context, user.Email);
            if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to delete a PBE BookList" }); }

            // We only ever soft delete a BookList but we do delete the maps. 

            // First we need to iterate through each BookList Map and delete them, these are a leaf node so this should be OK.
            foreach (QuizBookListBookMap book in BookList.QuizBookListBookMap)
            {
                _context.QuizBookListBookMap.Remove(book);
            }
            await _context.SaveChangesAsync();
            // Let's track this event 
            // _ = await Path.RegisterEventAsync(_context, EventType.PathDeleted, Path.Id.ToString());

            // Then we set the BookList to isDeleted
            if (BookList != null)
            {
                _context.Attach(BookList).State = EntityState.Modified;
                BookList.Modified = DateTime.Now;
                BookList.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./BookLists");
        }
    }
}
