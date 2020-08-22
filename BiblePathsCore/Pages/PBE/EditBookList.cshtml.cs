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
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BiblePathsCore.Pages.PBE
{
    [Authorize]
    public class EditBookListModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public EditBookListModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public QuizBookLists BookList { get; set; }

        [BindProperty]
        public List<QuizBookListBookMap> Books { get; set;  }

        [BindProperty] 
        public String BibleId { get; set; }
        public QuizUsers PBEUser { get; set; }
        [PageRemote(
            ErrorMessage = "Sorry, this Name is not valid, ",
            AdditionalFields = "__RequestVerificationToken, BookList.BookListName",
            HttpMethod = "post",
            PageHandler = "CheckName"
        )]
        [BindProperty]
        public string Name { get; set; }

        public async Task<IActionResult> OnGetAsync(string BibleId, int Id)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUsers.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a PBE BookList" }); }

            BookList = await _context.QuizBookLists.FindAsync(Id);
            if (BookList == null ) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Book List" }); }

            this.BibleId = await Bibles.GetValidPBEBibleIdAsync(_context, BibleId);

            //Initialize Books
            await _context.Entry(BookList).Collection(L => L.QuizBookListBookMap).LoadAsync();
            BookList.PadBookListBookMapsForEdit();
            Books = BookList.QuizBookListBookMap.ToList();
            Name = BookList.BookListName;
            ViewData["BookSelectList"] = await BibleBooks.GetBookSelectListAsync(_context, BibleId);
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int Id)
        {
            // confirm our user is a valid PBE User. 
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUsers.GetOrAddPBEUserAsync(_context, user.Email);
            if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a PBE BookList" }); }

            QuizBookLists BookListToUpdate = await _context.QuizBookLists.FindAsync(Id);
            if (BookListToUpdate == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Book List" }); }
            await _context.Entry(BookListToUpdate).Collection(L => L.QuizBookListBookMap).LoadAsync();

            // We need a copy of the BookListMap to compare to while the origonal is being updated.
            List<QuizBookListBookMap> CompareMap = BookListToUpdate.QuizBookListBookMap.ToList();

            BibleId = await Bibles.GetValidPBEBibleIdAsync(_context, BibleId);

            // Is this an attempted name change... for reals? 
            if (Name.ToLower() != BookList.BookListName.ToLower())
            {
                if (await QuizBookLists.ListNameAlreadyExistsStaticAsync(_context, Name))
                {
                    ModelState.AddModelError("Name", "Sorry, this Name is already in use.");
                }
                // Update the name since this is a rename attempt.
                BookListToUpdate.BookListName = Name;
            }

            if (!ModelState.IsValid)
            {
                BookList.PadBookListBookMapsForEdit();
                Books = BookList.QuizBookListBookMap.ToList();
                Name = BookList.BookListName;
                ViewData["BookSelectList"] = await BibleBooks.GetBookSelectListAsync(_context, BibleId);
                return Page();
            }

            _context.Attach(BookListToUpdate);
            BookListToUpdate.Modified = DateTime.Now;

            // now we need to update the books
            foreach (QuizBookListBookMap Book in Books)
            {
                // See if this is one of our existing BookMaps
                bool ExistingBookMap = true;
                QuizBookListBookMap OriginalBook = new QuizBookListBookMap();
                try
                {
                    OriginalBook = CompareMap.Where(B => B.Id == Book.Id).Single();
                }
                catch
                {
                    ExistingBookMap = false;
                    // New Book Scenario let's add the book. 
                    // But first we won't add it if the Book is already in the List. 
                    if (BookListToUpdate.QuizBookListBookMap.Where(B => B.BookNumber == Book.BookNumber).Any())
                    {
                        // Dupe scenario, don't add
                    }
                    else
                    {
                        if(Book.BookNumber > 0)
                        {
                            QuizBookListBookMap BookToAdd = new QuizBookListBookMap();
                            BookToAdd.BookList = BookListToUpdate;
                            BookToAdd.IsDeleted = false;
                            BookToAdd.Created = DateTime.Now;
                            BookToAdd.Modified = DateTime.Now;
                            BookToAdd.BookNumber = Book.BookNumber;
                            _context.QuizBookListBookMap.Add(BookToAdd);
                            //await _context.SaveChangesAsync();
                        }

                    }

                }
                if (ExistingBookMap)
                {
                    // This is the update BookMap scenario
                    if (OriginalBook.BookNumber != Book.BookNumber)
                    {
                        if (Book.BookNumber != 0)
                        {
                            _context.Attach(OriginalBook);
                            OriginalBook.Modified = DateTime.Now;
                            OriginalBook.BookNumber = Book.BookNumber;
                            // await _context.SaveChangesAsync();
                        }
                        else
                        {
                            // This is the delete BookMap scenario
                            _context.QuizBookListBookMap.Remove(OriginalBook);
                            // await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            await _context.SaveChangesAsync();
            return RedirectToPage("BookLists", new { BibleId = this.BibleId, Message = String.Format("Book List {0} successfully updated...", BookListToUpdate.BookListName) });
        }

        public async Task<JsonResult> OnPostCheckNameAsync()
        {
            // Is this an attempted name change... for reals? 
            if (Name.ToLower() != BookList.BookListName.ToLower())
            {
                if (await QuizBookLists.ListNameAlreadyExistsStaticAsync(_context, Name))
                {
                    return new JsonResult("Sorry, this Name is already in use.");
                }
            }
            return new JsonResult(true);
        }
    }
}
