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
    public class CreateModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public CreateModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public List<SelectListItem> BibleSelectList { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            BibleSelectList = await GetBibleSelectListAsync();
            return Page();
        }

        [BindProperty]
        public Path Path { get; set; }

        [PageRemote(
            ErrorMessage = "Sorry, this Name is not valid, ",
            AdditionalFields = "__RequestVerificationToken",
            HttpMethod = "post",
            PageHandler = "CheckName"
        )]
        [BindProperty]
        public string Name { get; set; }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            BibleSelectList = await GetBibleSelectListAsync();


            Path.Name = Name;

            if (await Path.PathNameAlreadyExistsStaticAsync(_context, Name))
            {
                ModelState.AddModelError("Name", "Sorry, this Name is already in use.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var emptyPath = new Path();
            var user = await _userManager.GetUserAsync(User);
            emptyPath.SetInitialProperties(user.Email);

            if (await TryUpdateModelAsync<Path>(
                emptyPath,
                "Path",   // Prefix for form value.
                p => p.IsPublicEditable, p => p.OwnerBibleId))
            {
                emptyPath.Name = Name;
                _context.Paths.Add(emptyPath);
                await _context.SaveChangesAsync();

                return RedirectToPage("./Steps", new { PathId = emptyPath.Id });
            }

            return Page();
        }

        public async Task<JsonResult> OnPostCheckNameAsync()
        {
            if (await Path.PathNameAlreadyExistsStaticAsync(_context, Name))
            {
                return new JsonResult("Sorry, this Name is already in use.");
            }
            return new JsonResult(true);
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
    }
}
