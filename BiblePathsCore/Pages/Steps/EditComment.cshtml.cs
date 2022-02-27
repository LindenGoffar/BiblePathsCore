using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Identity;

namespace BiblePathsCore
{
    public class EditCommentModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public EditCommentModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public Path Path { get; set; }

        [BindProperty]
        public PathNode Step { get; set; }

        [BindProperty]
        public string BibleId { get; set; }
        public async Task<IActionResult> OnGetAsync(string BibleId, int Id, int PathId, int? BookNumber, int? Chapter)
        {
            Step = await _context.PathNodes.FindAsync(Id);
            Path = await _context.Paths.FindAsync(PathId);
            BibleId = await Path.GetValidBibleIdAsync(_context, BibleId);

            //Do path and step exist? if not we've got an error. 
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Path" }); }
            if (Step == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Step" }); }
            if (Step.PathId != Path.Id) { return RedirectToPage("/error", new { errorMessage = "That's Odd! Path/Step mismatch" }); }

            // Handle the Change Book & Chapter Scenario ... this doesn't modify the DB, just the in memory instance of Step.
            if (BookNumber.HasValue && Chapter.HasValue)
            {
                Step.BookNumber = (int)BookNumber;
                Step.Chapter = (int)Chapter;
                Step.StartVerse = 1;
                Step.EndVerse = 1;
            }

            // confirm our owner is a valid path editor i.e. owner or the path is publicly editable
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (!Path.IsValidPathEditor(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add to this Path" }); }

            // Setup our Step for display
            _ = await Step.AddBookNameAsync(_context, BibleId);
            Step.Verses = await Step.GetBibleVersesAsync(_context, BibleId, false, false);

            // and now we need a Verse Select List
            ViewData["VerseSelectList"] = new SelectList(Step.Verses, "Verse", "Verse");
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int Id)
        {
            if (!ModelState.IsValid)
            {
                // Populate Step for display
                _ = await Step.AddBookNameAsync(_context, BibleId);
                Step.Verses = await Step.GetBibleVersesAsync(_context, BibleId, false, false);

                // and now we need a Verse Select List
                ViewData["VerseSelectList"] = new SelectList(Step.Verses, "Verse", "Verse");
                return Page();
            }
            // Now let's validate a few things, first lets go grab the Step and Path
            PathNode StepToUpdate = await _context.PathNodes.FindAsync(Id);
            Path = await _context.Paths.FindAsync(Step.PathId);
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Path" }); }
            if (StepToUpdate == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Step" }); }
            if (StepToUpdate.PathId != Path.Id) { return RedirectToPage("/error", new { errorMessage = "That's Odd! Path/Step mismatch" }); }

            BibleId = await Path.GetValidBibleIdAsync(_context, BibleId);
            // confirm our owner is a valid path editor i.e. owner or the path is publicly editable
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (!Path.IsValidPathEditor(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add to this Path" }); }

            if (!Path.IsPathOwner(user.Email))
            {
                _ = await Path.RegisterEventAsync(_context, EventType.NonOwnerEdit, user.Email);
            }

            if (await TryUpdateModelAsync<PathNode>(
                            StepToUpdate,
                            "Step",   // Prefix for form value.
                            S => S.StartVerse, S => S.EndVerse, S => S.BookNumber, S => S.Chapter))
            {
                StepToUpdate.Modified = DateTime.Now;
                await _context.SaveChangesAsync();

                // Now we need to update the Path Object with some calculated properties 
                if (!Path.IsPathOwner(user.Email)) { _ = await Path.RegisterEventAsync(_context, EventType.NonOwnerEdit, user.Email); }

                // Prepare to update some properties on Path
                _context.Attach(Path);
                Path.Modified = DateTime.Now;
                // Save our now updated Path Object. 
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Paths/Steps", new { PathId = Path.Id });
        }

        //private bool PathNodesExists(int id)
        //{
        //    return _context.PathNodes.Any(e => e.Id == id);
        //}
    }
}
