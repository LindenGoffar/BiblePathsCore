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

namespace BiblePathsCore
{
    [Authorize]
    public class StepsModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public StepsModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IList<PathNodes> PathNodes { get;set; }
        public Paths Path { get; set; }
        public bool IsPathOwner { get; set; }
        public Bibles Bible { get; set; }

        public async Task<IActionResult> OnGetAsync(int PathId)
        {
            // Confirm Path 
            Path = await _context.Paths.FindAsync(PathId);
            if (Path == null) { return NotFound(); }
            var user = await _userManager.GetUserAsync(User);
            IsPathOwner = Path.IsPathOwner(user.Email);

            // in this case we don't accept a BibleId on URL, instead we use the one supplied by the owner. 
            Bible = await _context.Bibles.FindAsync(Path.OwnerBibleId);

            PathNodes = await _context.PathNodes
                .Where(pn => pn.PathId == Path.Id).OrderBy(pn => pn.Position).ToListAsync();
            // Add our Bible Verse Data to each node. 
            foreach (PathNodes step in PathNodes)
            {
                _ = await step.AddBookNameAsync(_context, Bible.Id);
                step.Verses = await step.GetBibleVersesAsync(_context, Bible.Id, true, false);
            }
            return Page();
        }
    }
}
