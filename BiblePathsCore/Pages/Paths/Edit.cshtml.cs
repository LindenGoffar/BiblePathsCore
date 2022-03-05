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
        public Path Path { get; set; }

        [BindProperty]
        public List<SelectListItem> BibleSelectList { get; set; }

        [PageRemote(
            ErrorMessage = "Sorry, this Name is not valid, ",
            AdditionalFields = "__RequestVerificationToken, Path.Name",
            HttpMethod = "post",
            PageHandler = "CheckName"
        )]
        [BindProperty]
        public string Name { get; set; }

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
            Name = Path.Name;
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

            var pathToUpdate = await _context.Paths.FindAsync(id);

            // Is this an attempt to change the name? If so check the name. 
            if (pathToUpdate.Name.ToLower() != Name.ToLower())
            {
                if (await Path.PathNameAlreadyExistsStaticAsync(_context, Name))
                {
                    ModelState.AddModelError("Name", "Sorry, this Name is already in use.");
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }
            pathToUpdate.Modified = DateTime.Now;

            if (pathToUpdate.IsPublished)
            {
                if (await TryUpdateModelAsync<Path>(
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
                if (await TryUpdateModelAsync<Path>(
                        pathToUpdate,
                        "Path",
                        p => p.Name, p => p.OwnerBibleId, p => p.Topics, p => p.IsPublicEditable))
                {
                    pathToUpdate.Name = Name; // This is handled independant of Path.Name. 
                    await _context.SaveChangesAsync();
                    return RedirectToPage("./MyPaths");
                }
            }
            return Page();
        }

        public async Task<JsonResult> OnPostCheckNameAsync()
        {
            // Is this an attempted name change... for reals? 
            if(Path.Name != null)
            {
                if (Name.ToLower() != Path.Name.ToLower())
                {
                    if (await Path.PathNameAlreadyExistsStaticAsync(_context, Name))
                    {
                        return new JsonResult("Sorry, this Name is already in use.");
                    }
                }
            }
            return new JsonResult(true);
        }

        private async Task<List<SelectListItem>> GetBibleSelectListAsync()
        {
            // NOTE: Upon Further Review NKJV-EN is removed for stricter adherence to Thomas Nelson copyright.  
            return await _context.Bibles.Where(b => b.Id != "NKJV-EN").Select(b =>
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
