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
    public class EditModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public EditModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        [BindProperty]
        public Paths Path { get; set; }
        [BindProperty]
        public List<SelectListItem> BibleSelectList { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Path = await _context.Paths.FindAsync(id);

            if (Path == null)
            {
                return NotFound();
            }

            // confirm Path Owner
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (!Path.IsPathOwner(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Path Owner is allowed to edit these Path settings..." }); }
            
            BibleSelectList = await _context.Bibles.Select(b =>
                  new SelectListItem
                  {
                      Value = b.Id,
                      Text = b.Language + "-" + b.Version
                  }).ToListAsync();

            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var pathToUpdate = await _context.Paths.FindAsync(id);

            pathToUpdate.Modified = DateTime.Now;

            if (await TryUpdateModelAsync<Paths>(
                pathToUpdate,
                "Path",
                p => p.Name, p => p.OwnerBibleId, p => p.Topics, p => p.IsPublicEditable))
            {
                await _context.SaveChangesAsync();
                return RedirectToPage("./MyPaths");
            }

            return Page();

        }

        private bool PathsExists(int id)
        {
            return _context.Paths.Any(e => e.Id == id);
        }
    }
}
