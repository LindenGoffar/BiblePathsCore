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
    public class AddCommentaryModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public AddCommentaryModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public CommentaryBook Commentary { get; set; }
        public QuizUser PBEUser { get; set; }

        public async Task<IActionResult> OnGetAsync(string BibleId)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (!PBEUser.IsQuizModerator()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a Commentary entry." }); }

            Commentary = new CommentaryBook();

            Commentary.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            //Initialize Book Select List 
            ViewData["BookSelectList"] = await BibleBook.GetCommentaryBookSelectListAsync(_context, Commentary.BibleId, 0, false);
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            // Setup our PBEBible and Book Objects
            Commentary.BibleId = await QuizQuestion.GetValidBibleIdAsync(_context, Commentary.BibleId);
            BibleBook PBEBook = await BibleBook.GetPBEBookAndChapterAsync(_context, Commentary.BibleId, Commentary.BookNumber, 1); // we will assume a chapter 1 so let's go with it. 
            if (PBEBook == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find the PBE Book." }); }

            if (Commentary.BookName.IsNullOrEmpty()) { Commentary.BookName = PBEBook.Name; };

            if (!ModelState.IsValid)
            {
                //Initialize Book Select List 
                ViewData["BookSelectList"] = await BibleBook.GetCommentaryBookSelectListAsync(_context, Commentary.BibleId, 0, false);
                return Page();
            }

            // confirm our user is a valid PBE User and Moderator. 
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (User != null)
            {
                PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);
                if (!PBEUser.IsQuizModerator()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a Commentary." }); }
            }
            else { return RedirectToPage("/error", new { errorMessage = "Oops! We were unable to get our User Object from the UserManager, this Commentary cannot be added!" }); }

            // Now let's create an empty commentary and put only our validated properties onto it. 
            var emptyCommentary = new CommentaryBook()
            {
                Created = DateTime.Now,
                Modified = DateTime.Now,
            };

            if (await TryUpdateModelAsync<CommentaryBook>(
                emptyCommentary,
                "Commentary",   // Prefix for form value.
                C => C.BibleId, C => C.CommentaryTitle, C => C.BookNumber, C => C.BookName, C => C.Text))
            {
                emptyCommentary.Owner = PBEUser.Email;
                _context.CommentaryBooks.Add(emptyCommentary);

                await _context.SaveChangesAsync();

                return RedirectToPage("Commentaries", new { BibleId = Commentary.BibleId, Message = String.Format("Commentary for {0} successfully created...", PBEBook.Name) });
            }
            else { return RedirectToPage("/error", new { errorMessage = "Oops! We failed to update the Commentary Model, this Commentary cannot be added!" }); }
        }
    }
}
