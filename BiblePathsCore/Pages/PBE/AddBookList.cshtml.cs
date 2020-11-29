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

namespace BiblePathsCore.Pages.PBE
{
    [Authorize]
    public class AddBookListModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public AddBookListModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public int[] Books { get; set;  }

        [BindProperty] 
        public String BibleId { get; set; }
        public QuizUser PBEUser { get; set; }
        [PageRemote(
            ErrorMessage = "Sorry, this Name is not valid, ",
            AdditionalFields = "__RequestVerificationToken",
            HttpMethod = "post",
            PageHandler = "CheckName"
        )]
        [BindProperty]
        public string Name { get; set; }

        public async Task<IActionResult> OnGetAsync(string BibleId)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (!PBEUser.IsQuizModerator()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a PBE BookList" }); }
          
            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            //Initialize Books
            Books = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            ViewData["BookSelectList"] = await BibleBook.GetBookSelectListAsync(_context, BibleId);
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (await QuizBookList.ListNameAlreadyExistsStaticAsync(_context, Name))
            {
                ModelState.AddModelError("Name", "Sorry, this Name is already in use.");
            }
            if (!ModelState.IsValid)
            {
                ViewData["BookSelectList"] = await BibleBook.GetBookSelectListAsync(_context, BibleId);

                return Page();
            }

            // confirm our user is a valid PBE User. 
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);
            if (!PBEUser.IsQuizModerator()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a PBE BookList" }); }

            // Now let's create an empty BookList
            var emptyBookList = new QuizBookList
            {
                Created = DateTime.Now,
                Modified = DateTime.Now
            };

            emptyBookList.BookListName = Name;
            emptyBookList.IsDeleted = false;
            _context.QuizBookLists.Add(emptyBookList);

            // now we need to add the Books
            foreach (int BookNum in Books)
            {
                if (BookNum > 0)
                {
                    QuizBookListBookMap BookMap = new QuizBookListBookMap();
                    BookMap.BookList = emptyBookList;
                    BookMap.BookNumber = BookNum;
                    BookMap.IsDeleted = false;
                    BookMap.Created = DateTime.Now;
                    BookMap.Modified = DateTime.Now;
                    _context.QuizBookListBookMaps.Add(BookMap);
                    await _context.SaveChangesAsync();
                }
            }
            await _context.SaveChangesAsync();

            return RedirectToPage("BookLists", new { BibleId = this.BibleId, Message = String.Format("Book List {0} successfully created...", emptyBookList.BookListName) });
        }

        public async Task<JsonResult> OnPostCheckNameAsync()
        {
            if (await QuizBookList.ListNameAlreadyExistsStaticAsync(_context, Name))
            {
                return new JsonResult("Sorry, this Name is already in use.");
            }
            return new JsonResult(true);
        }
    }
}
