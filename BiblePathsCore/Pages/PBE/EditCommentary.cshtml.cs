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
using Microsoft.IdentityModel.Tokens;

namespace BiblePathsCore.Pages.PBE
{
    [Authorize]
    public class EditCommentaryModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public EditCommentaryModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public CommentaryBook Commentary { get; set; }
        public QuizUser PBEUser { get; set; }

        public async Task<IActionResult> OnGetAsync(string BibleId, int Id)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (!PBEUser.IsQuizModerator()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to edit this Commentary entry." }); }

            Commentary = await _context.CommentaryBooks.FindAsync(Id);
            if (Commentary == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Commentary entry" }); }

            Commentary.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            //Initialize Book Select List 
            ViewData["BookSelectList"] = await BibleBook.GetCommentaryBookSelectListAsync(_context, Commentary.BibleId, Commentary.BookNumber, false);
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int Id)
        {
            CommentaryBook CommentaryToUpdate = await _context.CommentaryBooks.FindAsync(Id);
            if (CommentaryToUpdate == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Commentary entry" }); }
            
            // Setup our PBEBible and Book Objects
            BibleBook PBEBook = await BibleBook.GetPBEBookAndChapterAsync(_context, Commentary.BibleId, Commentary.BookNumber, 1); // we will assume a chapter 1 so let's go with it. 
            if (PBEBook == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find the PBE Book." }); }

            if (Commentary.BookName.IsNullOrEmpty()) { Commentary.BookName = PBEBook.Name; };

            if (!ModelState.IsValid)
            {
                //Initialize Book Select List 
                ViewData["BookSelectList"] = await BibleBook.GetCommentaryBookSelectListAsync(_context, Commentary.BibleId, Commentary.BookNumber, false);
                return Page();
            }

            // confirm our user is a valid PBE User and Moderator. 
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (User != null)
            {
                PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);
                if (!PBEUser.IsQuizModerator()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to edit this Commentary." }); }
            }
            else { return RedirectToPage("/error", new { errorMessage = "Oops! We were unable to get our User Object from the UserManager, this Commentary cannot be added!" }); }

            if (await TryUpdateModelAsync<CommentaryBook>(
                CommentaryToUpdate,
                "Commentary",   // Prefix for form value.
                C => C.BibleId, C => C.CommentaryTitle, C => C.BookNumber, C => C.BookName, C => C.Text))
            {
                CommentaryToUpdate.Modified = DateTime.Now;
                await _context.SaveChangesAsync();
                return RedirectToPage("Commentaries", new { BibleId = Commentary.BibleId, Message = String.Format("Commentary for {0} successfully updated...", PBEBook.Name) });
            }
            else { return RedirectToPage("/error", new { errorMessage = "Oops! We failed to update the Commentary Model, this Commentary cannot be added!" }); }
        }
    }
}
