﻿using System;
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
            BibleSelectList = await _context.Bibles.Select(b =>
                              new SelectListItem
                              {
                                  Value = b.Id,
                                  Text = b.Language + "-" + b.Version
                              }).ToListAsync();
            return Page();
        }

        [BindProperty]
        public Paths Path { get; set; }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var emptyPath = new Paths();
            var user = await _userManager.GetUserAsync(User);
            emptyPath.SetInitialProperties(user.Email);

            if (await TryUpdateModelAsync<Paths>(
                emptyPath,
                "Path",   // Prefix for form value.
                p => p.Name, p => p.IsPublicEditable, p => p.OwnerBibleId))
            {
                _context.Paths.Add(emptyPath);
                await _context.SaveChangesAsync();

                return RedirectToPage("./MyPaths");
            }

            return Page();
        }
    }
}