using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            BibleSelectList = await GetBibleSelectListAsync();
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

            BibleSelectList = await GetBibleSelectListAsync();
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var pathToUpdate = await _context.Paths.FindAsync(id);

            pathToUpdate.Modified = DateTime.Now;

            if (pathToUpdate.IsPublished)
            {
                if (await TryUpdateModelAsync<Paths>(
                    pathToUpdate,
                     "Path",
                     p => p.OwnerBibleId, p => p.IsPublicEditable))
                {
                    await _context.SaveChangesAsync();
                    return RedirectToPage("./MyPaths");
                }
            }
            else
            {
                if (await TryUpdateModelAsync<Paths>(
                        pathToUpdate,
                        "Path",
                        p => p.Name, p => p.OwnerBibleId, p => p.Topics, p => p.IsPublicEditable))
                {
                    await _context.SaveChangesAsync();
                    return RedirectToPage("./MyPaths");
                }
            }
            return Page();
        }

        private async Task<List<SelectListItem>> GetBibleSelectListAsync()
        {
            return await _context.Bibles.Select(b =>
                              new SelectListItem
                              {
                                  Value = b.Id,
                                  Text = b.Language + "-" + b.Version
                              }).ToListAsync();
        }

        private bool PathsExists(int id)
        {
            return _context.Paths.Any(e => e.Id == id);
        }
    }
}
