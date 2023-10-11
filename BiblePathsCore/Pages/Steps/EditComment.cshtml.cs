using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        public async Task<IActionResult> OnGetAsync(int Id, int PathId)
        {
            Step = await _context.PathNodes.FindAsync(Id);
            Path = await _context.Paths.FindAsync(PathId);

            //Do path and step exist? if not we've got an error. 
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Path" }); }
            if (Step == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Step" }); }
            if (Step.PathId != Path.Id) { return RedirectToPage("/error", new { errorMessage = "That's Odd! Path/Step mismatch" }); }

            // confirm our owner is a valid path editor i.e. owner or the path is publicly editable
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (!Path.IsValidPathEditor(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to edit to this Path" }); }
            
            // Confirm this Path indeed supports Comments
            if (Path.Type != (int)PathType.Commented) { return RedirectToPage("/error", new { errorMessage = "Sorry! This Path does not support Steps of type Comment" }); }

            // We want to force Comment changes through the Publish flow so no adding/editing comments on published paths. 
            if (Path.IsPublished) { return RedirectToPage("/error", new { errorMessage = "Sorry! Comments can only be added/edited in UnPublished Paths" }); }

            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int Id)
        {
            // Sanity check Step.Text
            ContentReview CheckThis = new ContentReview(Step.Text);
            if (CheckThis.FindBannedWords() > 0)
            {
                string WordsFound = string.Join(", ", CheckThis.FoundWords);
                string ErrorMessage = "This is awkward, but we feel some of the words used below are not appropriate for the Bible Paths mission, these include: " + WordsFound;
                ModelState.AddModelError(string.Empty, ErrorMessage);
                ModelState.AddModelError("Step.Text", "Please review the text below for inappropriate words...");
            }

            // Now let's validate a few things, first lets go grab the Step and Path
            PathNode StepToUpdate = await _context.PathNodes.FindAsync(Id);
            Path = await _context.Paths.FindAsync(Step.PathId);
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Path" }); }
            if (StepToUpdate == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Step" }); }
            if (StepToUpdate.PathId != Path.Id) { return RedirectToPage("/error", new { errorMessage = "That's Odd! Path/Step mismatch" }); }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // confirm our owner is a valid path editor i.e. owner or the path is publicly editable
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (!Path.IsValidPathEditor(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add to this Path" }); }
            
            // Confirm this Path indeed supports Comments
            if (Path.Type != (int)PathType.Commented) { return RedirectToPage("/error", new { errorMessage = "Sorry! This Path does not support Steps of type Comment" }); }

            // We want to force Comment changes through the Publish flow so no adding/editing comments on published paths. 
            if (Path.IsPublished) { return RedirectToPage("/error", new { errorMessage = "Sorry! Comments can only be added/edited in UnPublished Paths" }); }

            if (await TryUpdateModelAsync<PathNode>(
                            StepToUpdate,
                            "Step",   // Prefix for form value.
                            S => S.Text))
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
            return RedirectToPage("/CommentedPaths/Steps", new { PathId = Path.Id });
        }
    }
}
