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
    public class AddBuilderCommentModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public AddBuilderCommentModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public Path Path { get; set; }
        public PathNode Step { get; set; }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int PathId, int Position)
        {
            Path = await _context.Paths.FindAsync(PathId);

            //Does the Path exist? if not we've got an error. 
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Path" }); }

            // confirm our owner is a valid path editor i.e. owner or the path is publicly editable
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (!Path.IsValidPathEditor(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to edit to this Path" }); }

            // Confirm this Path indeed supports Comments
            if (Path.Type != (int)PathType.Commented) { return RedirectToPage("/error", new { errorMessage = "Sorry! This Path does not support Steps of type Comment" }); }

            // We want to force Comment changes through the Publish flow so no adding/editing comments on published paths. 
            if (Path.IsPublished) { return RedirectToPage("/error", new { errorMessage = "Sorry! Comments can only be added/edited in UnPublished Paths" }); }

            Step = new PathNode();
            Step.PathId = Path.Id;
            Step.Created = DateTime.Now;
            Step.Modified = DateTime.Now;
            Step.Type = (int)PathType.Commented;
            Step.Owner = user.Email;
            // What we get for Position is actually that of the previous node in the path.
            // We want to replace that with a temporary position that we'll update later. 
            Step.Position = Position + 5;

            // Really not sure this is still viable functionality but we'll stick with it.
            if (!Path.IsPathOwner(user.Email))
            {
                _ = await Path.RegisterEventAsync(_context, EventType.NonOwnerEdit, user.Email);
            }
            _context.PathNodes.Add(Step);
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
                
            return RedirectToPage("/CommentedPaths/Builder", new { PathId = Path.Id, StepPosition = Step.Position });
        }
    }
}
