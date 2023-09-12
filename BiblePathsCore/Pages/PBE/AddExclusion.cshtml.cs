using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BiblePathsCore.Pages.PBE
{
    [Authorize]
    public class AddExclusionModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public AddExclusionModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public PBEExclusion Exclusion { get; set; }
        public QuizUser PBEUser { get; set; }
        public int ChapterVerseCount { get; set; }

        public async Task<IActionResult> OnGetAsync(string BibleId, int BookNumber, int Chapter)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (!PBEUser.IsQuizModerator()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a PBE Exclusion" }); }

            Exclusion = new PBEExclusion();
            // Setup our PBEBook Object
            Exclusion.BibleId = await PBEExclusion.GetValidBibleIdAsync(_context, BibleId);
            Exclusion.BookNumber = BookNumber;
            Exclusion.Chapter = Chapter;
            Exclusion.StartVerse = 1; // set to 1 if VersNum is Null.
            Exclusion.EndVerse = 1; // set to 1 if VersNum is Null.

            BibleBook PBEBook = await BibleBook.GetPBEBookAndChapterAsync(_context, Exclusion.BibleId, Exclusion.BookNumber, Exclusion.Chapter);
            if (PBEBook == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find the PBE Book." }); }
            // Now that we have Book Details let's add BookName to the exclusion
            Exclusion.BookName = PBEBook.Name;

            if (Exclusion.Chapter == Bible.CommentaryChapter) { return RedirectToPage("/error", new { errorMessage = "Sorry! You can not add Exclusions on the Commentary content" }); }

            ChapterVerseCount = (int)PBEBook.BibleChapters.Where(c => c.ChapterNumber == Exclusion.Chapter).First().Verses;
            // and now we need a Verse Select List
            ViewData["VerseSelectList"] = Exclusion.GetVerseNumSelectList(ChapterVerseCount);
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            // Setup our PBEBible and Book Objects
            Exclusion.BibleId = await QuizQuestion.GetValidBibleIdAsync(_context, Exclusion.BibleId);
            BibleBook PBEBook = await BibleBook.GetPBEBookAndChapterAsync(_context, Exclusion.BibleId, Exclusion.BookNumber, Exclusion.Chapter);
            if (PBEBook == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find the PBE Book." }); }

            Exclusion.BookName = PBEBook.Name;

            if (Exclusion.Chapter == Bible.CommentaryChapter) { return RedirectToPage("/error", new { errorMessage = "Sorry! You can not add Exclusions on the Commentary content" }); }

            if (!ModelState.IsValid)
            {
                ChapterVerseCount = (int)PBEBook.BibleChapters.Where(c => c.ChapterNumber == Exclusion.Chapter).First().Verses;
                // and now we need a Verse Select List
                ViewData["VerseSelectList"] = Exclusion.GetVerseNumSelectList(ChapterVerseCount);
                return Page();
            }

            // confirm our user is a valid PBE User. 
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (User != null)
            {
                PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);
                if (!PBEUser.IsQuizModerator()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a PBE Exclusion" }); }
            }
            else { return RedirectToPage("/error", new { errorMessage = "Oops! We were unable to get our User Object from the UserManager, this Exclusion cannot be added!" }); }

            // Now let's create an empty exclusion and put only our validated properties onto it. 
            // NOTE: At the DB level an Exclusion is an overly simplified QuizQuestion
            var emptyExclusion = new QuizQuestion
            {
                Created = DateTime.Now,
                Modified = DateTime.Now,
            };

            if (await TryUpdateModelAsync<QuizQuestion>(
                emptyExclusion,
                "Exclusion",   // Prefix for form value.
                E => E.BibleId, E => E.BookNumber, E => E.Chapter, E => E.StartVerse, E => E.EndVerse))
            {
                emptyExclusion.Owner = PBEUser.Email;
                emptyExclusion.Source = "BiblePaths.Net Automated Exclusion";
                emptyExclusion.Type = (int)QuestionType.Exclusion;
                _context.QuizQuestions.Add(emptyExclusion);

                await _context.SaveChangesAsync();

                return RedirectToPage("Exclusions", new { BibleId = emptyExclusion.BibleId, Message = String.Format("PBE Exclusion for {0}:{1} successfully created...", Exclusion.BookName, Exclusion.Chapter) });
            }
            else { return RedirectToPage("/error", new { errorMessage = "Oops! We failed to update the Exclusion Model, this Exclusion cannot be added!" }); }
        }
    }
}
