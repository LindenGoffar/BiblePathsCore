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

namespace BiblePathsCore
{
    [Authorize]
    public class AddCommentModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public AddCommentModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public Path Path { get; set; }

        [BindProperty]
        public PathNode Step { get; set; }

        public async Task<IActionResult> OnGetAsync(int PathId, int Position)
        {
            // Does the path exist? if not we've got an error. 
            Path = await _context.Paths.FindAsync(PathId);
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Path" }); }
            
            // Confirm our owner is a valid path editor i.e. owner or the path is publicly editable
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (!Path.IsValidPathEditor(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add to this Path" }); }
            
            // Confirm this Path indeed supports Comments
            if (Path.Type != (int)PathType.Commented) { return RedirectToPage("/error", new { errorMessage = "Sorry! This Path does not support Steps of type Comment" }); }

            // We want to force Comment changes through the Publish flow so no adding/editing comments on published paths. 
            if (Path.IsPublished) { return RedirectToPage("/error", new { errorMessage = "Sorry! Comments can only be added/edited in UnPublished Paths" }); }

            Step = new PathNode();
            Step.PathId = Path.Id;
            Step.Position = Position;

            ViewData["TargetPage"] = "AddComment";
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
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

            // Now let's validate a few things, first lets go grab the path.
            Path = await _context.Paths.FindAsync(Step.PathId);
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Path" }); }
            
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

            if (!Path.IsPathOwner(user.Email))
            {
                _ = await Path.RegisterEventAsync(_context, EventType.NonOwnerEdit, user.Email);
            }

            // Now let's create an empty Step aka. PathNode object so we can put only our validated properties onto it. 
            var emptyStep = new PathNode
            {
                Created = DateTime.Now,
                Modified = DateTime.Now,
                Type = (int)PathType.Commented,
                Owner = user.Email
            };

            if (await TryUpdateModelAsync<PathNode>(
                emptyStep,
                "Step",   // Prefix for form value.
                S => S.Text, S => S.PathId, S => S.Position))
            {
                // What we get for Position is actually that of the previous node in the path.
                // We want to replace that with a temporary position that we'll update later. 
                int PreviousNodePosition = emptyStep.Position;
                emptyStep.Position = PreviousNodePosition + 5;

                _context.PathNodes.Add(emptyStep);
                await _context.SaveChangesAsync();
                               
                // Prepare to update some properties on Path
                _context.Attach(Path);
                Path.Length = await Path.GetPathVerseCountAsync(_context);
                Path.StepCount = Path.PathNodes.Count;
                Path.Modified = DateTime.Now;
                // Save our now updated Path Object. 
                await _context.SaveChangesAsync();

                // Finally we need to re-position each node in the path to ensure safe ordering
                _ = await Path.RedistributeStepsAsync(_context);
                
                return RedirectToPage("/CommentedPaths/Steps", new { PathId = Path.Id });
            }
            return RedirectToPage("/CommentedPaths/Steps", new { PathId = Path.Id });
        }
    }
}
