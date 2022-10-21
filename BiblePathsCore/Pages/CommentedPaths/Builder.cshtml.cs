using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BiblePathsCore
{
    [Authorize]
    public class BuilderModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public BuilderModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public IList<PathNode> PathNodes { get;set; }
        public Path Path { get; set; }
        public bool IsPathOwner { get; set; }
        public bool IsPathEditor { get; set; }
        public Bible Bible { get; set; }
        public List<SelectListItem> BibleSelectList { get; set; }

        [BindProperty(SupportsGet = true)]
        public string BibleId { get; set; }

        public async Task<IActionResult> OnGetAsync(int PathId, int? StepId)
        {
            // Confirm Path 
            Path = await _context.Paths.FindAsync(PathId);
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We were unable to find the requested Commented Path" }); }

            // Validate our user 
            var user = await _userManager.GetUserAsync(User);
            IsPathOwner = Path.IsPathOwner(user.Email);
            IsPathEditor = Path.IsValidPathEditor(user.Email);

            // We want to use the Owners Bible ID only if BibleId hasn't been provided. 
            if (BibleId == null) { BibleId = Path.OwnerBibleId; }
            BibleId = await Path.GetValidBibleIdAsync(_context, BibleId);            
            Bible = await _context.Bibles.FindAsync(BibleId);
            if (Bible == null) { return RedirectToPage("/error", new { errorMessage = string.Format("That's Odd! We were unable to find the Bible: {0}", BibleId) }); }
            Bible.HydrateBible();

            PathNodes = await Path.GetPathNodesAsListAsync(_context, BibleId);

            BibleSelectList = await GetBibleSelectListAsync(BibleId);
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int PathId)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            // Let's get the path and validate a few things.
            Path = await _context.Paths.FindAsync(PathId);
            if (Path == null)
            {
                return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Path" });
            }
            // Load our PathNodes for referene later
            _context.Entry(Path).Collection(p => p.PathNodes).Load();

            // confirm our owner is a valid path editor i.e. owner or the path is publicly editable
            if (!Path.IsValidPathEditor(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add to this Path" }); }

            // Confirm this Path indeed supports Comments
            if (Path.Type != (int)PathType.Commented) { return RedirectToPage("/error", new { errorMessage = "Sorry! This Path does not support Steps of type Comment" }); }

            // We want to force Comment changes through the Publish flow so no adding/editing comments on published paths. 
            if (Path.IsPublished) { return RedirectToPage("/error", new { errorMessage = "Sorry! Comments can only be added/edited in UnPublished Paths" }); }

            // We should now have a list of partial PathNodes now, all are likely Comments
            // Sanity check each Comment Step
            foreach (PathNode Step in PathNodes)
            {
                // Now lets process each individual step.
                // We pull the step from DB to check whether it needs to be updated.
                PathNode StepToUpdate = Path.PathNodes.Where(N => N.Id == Step.Id).Single();
                if (StepToUpdate == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this step in the Database" }); }
                if (StepToUpdate.PathId != Path.Id) { return RedirectToPage("/error", new { errorMessage = "That's Odd! Path/Step mismatch" }); }

                // We really only want to update if there is a change to the Step Text.  
                if (Step.Text != StepToUpdate.Text)
                {
                    // Let's check for some not so good words
                    if (Step.Type == (int)StepType.Commented)
                    {
                        ContentReview CheckThis = new ContentReview(Step.Text);
                        if (CheckThis.FindBannedWords() > 0)
                        {
                            string WordsFound = string.Join(", ", CheckThis.FoundWords);
                            string ErrorMessage = "This is awkward, but we feel some of the words used below are not appropriate for the Bible Paths mission, these include: " + WordsFound;
                            ModelState.AddModelError(string.Empty, ErrorMessage);
                            ModelState.AddModelError("Step.Text", "Please review the text below for inappropriate words...");
                        }
                    }
                    // let's go ahead and fail if Model State is invalid, but first load some key elements for the page. 
                    if (!ModelState.IsValid)
                    {
                        IsPathOwner = this.Path.IsPathOwner(user.Email);
                        IsPathEditor = this.Path.IsValidPathEditor(user.Email);

                        BibleSelectList = await GetBibleSelectListAsync(BibleId);
                        return Page();
                    }
                    // Alright then we can now update our step.
                    StepToUpdate.Text = Step.Text;

                    StepToUpdate.Modified = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Prepare to update some properties on Path
                    _context.Attach(this.Path);
                    this.Path.Modified = DateTime.Now;
                    // Save our now updated Path Object. 
                    await _context.SaveChangesAsync();
                }
            }
            // If this is a non-owner edit let's log that... this functionality is not fully functional in my view. 
            if (!Path.IsPathOwner(user.Email))
            {
                _ = await Path.RegisterEventAsync(_context, EventType.NonOwnerEdit, user.Email);
            }
            return base.RedirectToPage("/CommentedPaths/Builder", new { PathId = this.Path.Id });
        }

        private async Task<List<SelectListItem>> GetBibleSelectListAsync(string BibleId)
        {
            // NOTE: Upon Further Review NKJV-EN is removed for stricter adherence to Thomas Nelson copyright.  
            return await _context.Bibles.Where(b => b.Id != "NKJV-EN").Select(b =>
                               new SelectListItem
                               {
                                   Value = b.Id,
                                   Text = b.Language + "-" + b.Version
                               }).ToListAsync();
        }
    }
}
