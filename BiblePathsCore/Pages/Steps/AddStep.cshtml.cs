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

namespace BiblePathsCore
{
    [Authorize]
    public class AddStepModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public AddStepModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<BibleVerses> Verses { get; set; }
        public Paths Path { get; set; }

        public async Task<IActionResult> OnGet(string BibleId, int PathId, int BookNumber, int Chapter, int? VerseNum)
        {
            //Does the path exist? if not we've got an error. 
            Paths Path = await _context.Paths.FindAsync(PathId);
            if (Path != null)
            {
                // confirm our owner is a valid path editor i.e. owner or the path is publicly editable
                IdentityUser user = await _userManager.GetUserAsync(User);
                if (Path.IsValidPathEditor(user.Email))
                {
                    ViewData["PathId"] = new SelectList(_context.Paths, "Id", "Id");
                    return Page();

                }
            }
            return RedirectToPage("/error");
        }

        [BindProperty]
        public PathNodes PathNodes { get; set; }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.PathNodes.Add(PathNodes);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
