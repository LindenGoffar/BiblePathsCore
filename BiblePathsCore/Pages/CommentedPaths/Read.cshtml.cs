using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BiblePathsCore
{
    // [Authorize]
    public class ReadModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public ReadModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IList<PathNode> PathNodes { get;set; }
        public Path Path { get; set; }
        public bool IsPathOwner { get; set; }
        public Bible Bible { get; set; }
        public int FocusStepID { get; set; }
        public List<SelectListItem> BibleSelectList { get; set; }

        [BindProperty(SupportsGet = true)]
        public string BibleId { get; set; }

        public async Task<IActionResult> OnGetAsync(int PathId, int? StepId, int MarkAsRead = 0)
        {
            bool CountAsRead = MarkAsRead == 1 ? true : false;
            IsPathOwner = false;

            // Confirm Path 
            Path = await _context.Paths.FindAsync(PathId);
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We were unable to find the requested Commented Path" }); }
            // Check whether user is Auth'd since we support either way. 
            if (User.Identity.IsAuthenticated){
                var user = await _userManager.GetUserAsync(User);
                IsPathOwner = Path.IsPathOwner(user.Email);
            }
            
            // We want to use the Owners Bible ID only if BibleId hasn't been provided. 
            //if (BibleId == null) { BibleId = Path.OwnerBibleId; }
            //BibleId = await Path.GetValidBibleIdAsync(_context, BibleId);
            _ = await Path.SetValidBibleIdAsync(_context, BibleId);

            Bible = await _context.Bibles.FindAsync(Path.BibleId);
            if (Bible == null) { return RedirectToPage("/error", new { errorMessage = string.Format("That's Odd! We were unable to find the Bible: {0}", Path.BibleId) }); }
            Bible.HydrateBible();

            // load Path Nodes for this Path, we already have BibleId set correctly on Path
            PathNodes = await Path.GetPathNodesAsListAsync(_context, true);

            //PathNodes = await _context.PathNodes.Where(pn => pn.PathId == Path.Id)
            //                                    .OrderBy(pn => pn.Position)
            //                                    .ToListAsync();

            // Add our Bible Verse and fwd/back step Data to each node. 
            // Much of this has already been done in GetPathNodesAsListAsync
            bool FocusStepSelected = false;
            int FirstStepID = 0;
            foreach (PathNode step in PathNodes)
            {
                //if (step.Type == (int)StepType.Commented)
                //{
                //    _ = await step.AddPathStepPropertiesAsync(_context);
                //}
                //else
                //{
                //    _ = await step.AddGenericStepPropertiesAsync(_context, BibleId);
                //    step.Verses = await step.GetBibleVersesAsync(_context, BibleId, true, false);
                //    _ = await step.AddPathStepPropertiesAsync(_context);
                //}
                // Now StepID is either Null or presumably a valid Step let's make sure get's set to a valid step.
                if (StepId == null) { StepId = step.Id; FirstStepID = step.Id; }
                if (StepId == step.Id) { FocusStepSelected = true; FocusStepID = step.Id; }
            }
            if (!FocusStepSelected) { FocusStepID = FirstStepID; }

            // Now let's register this as a Path Start and Path Read
            //if (MarkAsRead == 1)
            //{
            //    // _ = await Path.RegisterEventAsync(_context, EventType.PathStarted, null);
            //    // _ = await Path.RegisterEventAsync(_context, EventType.PathCompleted, null);
            //    _ = await Path.RegisterReadEventAsync(_context);
            //    // To keep the score somewhat fresh we'll recalculate score on every 10 reads.
            //    if (Path.Reads % 10 == 0)
            //    {
            //        _ = await Path.ApplyPathRatingAsync(_context);
            //    }
            //}

            BibleSelectList = await GetBibleSelectListAsync(BibleId);
            return Page();
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
