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
    // [Authorize]
    public class CPStepsModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public CPStepsModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IList<PathNode> PathNodes { get;set; }
        public Path Path { get; set; }
        public bool IsPathReader { get; set; } // Path Reader scenario overrides most Owner/Editor capabilities
        public bool IsPathOwner { get; set; }
        public bool IsPathEditor { get; set; }
        public Bible Bible { get; set; }
        public List<SelectListItem> BibleSelectList { get; set; }

        [BindProperty(SupportsGet = true)]
        public string BibleId { get; set; }

        //[BindProperty(SupportsGet = true)]
        //public bool? CountAsRead { get; set; }

        public async Task<IActionResult> OnGetAsync(int PathId, string Scenario, int MarkAsRead = 0)
        {
            //CountAsRead = CountAsRead.HasValue ? CountAsRead.Value : false;
            IsPathEditor = false;
            IsPathOwner = false;

            IsPathReader = Scenario switch
            {
                "ReadPath" => true,
                _ => false,
            };

            // Confirm Path 
            Path = await _context.Paths.FindAsync(PathId);
            if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We were unable to find the requested Commented Path" }); }
            // Check whether user is Auth'd since we support either way. 
            if (User.Identity.IsAuthenticated){
                var user = await _userManager.GetUserAsync(User);
                IsPathOwner = Path.IsPathOwner(user.Email);
                IsPathEditor = Path.IsValidPathEditor(user.Email);
            }
            
            // We want to use the Owners Bible ID only if BibleId hasn't been provided. 
            if (BibleId == null) { BibleId = Path.OwnerBibleId; }
            BibleId = await Path.GetValidBibleIdAsync(_context, BibleId);            

            Bible = await _context.Bibles.FindAsync(BibleId);
            if (Bible == null) { return RedirectToPage("/error", new { errorMessage = string.Format("That's Odd! We were unable to find the Bible: {0}", BibleId) }); }
            Bible.HydrateBible();

            PathNodes = await _context.PathNodes.Where(pn => pn.PathId == Path.Id)
                                                .OrderBy(pn => pn.Position)
                                                .ToListAsync();

            // Add our Bible Verse and fwd/back step Data to each node. 
            foreach (PathNode step in PathNodes)
            {
                if (step.Type == (int)StepType.Commented)
                {
                    _ = await step.AddPathStepPropertiesAsync(_context);
                }
                else
                {
                    _ = await step.AddGenericStepPropertiesAsync(_context, BibleId);
                    step.Verses = await step.GetBibleVersesAsync(_context, BibleId, true, false);
                    _ = await step.AddPathStepPropertiesAsync(_context);
                }
            }

            // Now let's conditionally register this as a Path Read
            if (MarkAsRead == 1)
            {
                //_ = Path.RegisterEventAsync(_context, EventType.PathStarted, null);
                //_ = await Path.RegisterEventAsync(_context, EventType.PathCompleted, null);
                _ = await Path.RegisterReadEventAsync(_context);
            }
            //if ((bool)CountAsRead) { _ = Path.RegisterEventAsync(_context, EventType.PathCompleted, null); };

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
