﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BiblePathsCore.Models.DB;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace BiblePathsCore
{
    public class PublishModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public PublishModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public Path Path { get; set; }

        [BindProperty]
        public string BibleId { get; set; }
        public List<BibleVerse> BibleVerses { get; set; }

        [PageRemote(
            ErrorMessage = "Sorry, this Name is not valid, ",
            AdditionalFields = "__RequestVerificationToken, Path.Name",
            HttpMethod = "post",
            PageHandler = "CheckName"
        )]
        [BindProperty]
        public string Name { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, string ReqBibleId)
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

            // confirm our owner is a valid path owner.
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (!Path.IsPathOwner(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Path Owner is allowed to publish a Path" }); }

            if (string.IsNullOrEmpty(ReqBibleId))
            {
                ReqBibleId = Path.OwnerBibleId;
            }

            BibleId = await Path.GetValidBibleIdAsync(_context, ReqBibleId);
            BibleVerses = await Path.GetPathVersesAsync(_context, BibleId);
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

            var pathToUpdate = await _context.Paths.FindAsync(id);

            if  (pathToUpdate == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We were unable to find this Path." }); }
            // Is this an attempt to change the name? If so check the name. 
            if (pathToUpdate.Name.ToLower() != Name.ToLower())
            {
                if (await Path.PathNameAlreadyExistsStaticAsync(_context, Name))
                {
                    ModelState.AddModelError("Name", "Sorry, this Name is already in use.");
                }
            }
            // confirm our owner is a valid path owner.
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (!pathToUpdate.IsPathOwner(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Path Owner is allowed to publish a Path" }); }

            // Try to update our model with the bound Name, Topic Properties. 
            BibleId = await Path.GetValidBibleIdAsync(_context, BibleId);
            BibleVerses = await Path.GetPathVersesAsync(_context, BibleId);

            if (await TryUpdateModelAsync<Path>(
                pathToUpdate,
                "Path",
                p => p.Name, p => p.Topics))
            {

                // Now We've got some necessary validation to do here so let's build the verse text block
                pathToUpdate.Name = Name; // Name is handled seperately for remote validation to work. 
                StringBuilder PathText = new StringBuilder();
                foreach (BibleVerse Verse in BibleVerses)
                {
                    PathText.Append(Verse.Text + " ");
                }
                String PathTextForCompare = PathText.ToString().ToLower();
                if (!PathTextForCompare.Contains(pathToUpdate.Name.ToLower()))
                {
                    //Invalidate the Model State by adding a Model Error
                    ModelState.AddModelError(string.Empty, "Please check Name and Topics");
                    ModelState.AddModelError("Name", "Sorry! The supplied name was not found in the text shown below. Please select a Path Name from Bible passages in this Path.");
                }
                if (pathToUpdate.Topics != null)
                {
                    string[] IndividualProposedTopics = pathToUpdate.Topics.Split(',');
                    foreach (string ProposedTopic in IndividualProposedTopics)
                    {
                        if (!(PathTextForCompare.Contains(ProposedTopic.ToLower())))
                        {
                            //Invalidate the Model State by adding a Model Error
                            ModelState.AddModelError(string.Empty, "Please check Name and Topics");
                            string ErrorMessage = "Sorry! The following Topic was not found in the Bible passages in this Path: " + ProposedTopic;
                            ModelState.AddModelError("Path.Topics", ErrorMessage);
                        }
                    }
                }
                if (!ModelState.IsValid)
                {
                    //BibleVerses is already set
                    return Page();
                }

                _context.Attach(pathToUpdate).State = EntityState.Modified;
                pathToUpdate.Modified = DateTime.Now;
                pathToUpdate.IsPublished = true;
                await _context.SaveChangesAsync();

                return RedirectToPage("./MyPaths");

            }

            //BibleVerses set above
            return Page();
        }

        //public async Task<IActionResult> OnPostUnPublishAsync(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        return RedirectToPage("/error", new { errorMessage = "That's Odd! Model State is not valid, I can't explain it either... " });
        //    }

        //    var pathToUpdate = await _context.Paths.FindAsync(id);

        //    if (pathToUpdate == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We were unable to find this Path." }); }

        //    // confirm our owner is a valid path owner.
        //    IdentityUser user = await _userManager.GetUserAsync(User);
        //    if (!pathToUpdate.IsPathOwner(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Path Owner is allowed to publish a Path" }); }

        //    _context.Attach(pathToUpdate).State = EntityState.Modified;
        //    pathToUpdate.Modified = DateTime.Now;
        //    pathToUpdate.IsPublished = false;
        //    await _context.SaveChangesAsync();

        //    return RedirectToPage("./MyPaths");
        //}

        public async Task<JsonResult> OnPostCheckNameAsync()
        {
            // Is this an attempted name change... for reals? 
            if (Name.ToLower() != Path.Name.ToLower())
            {
                if (await Path.PathNameAlreadyExistsStaticAsync(_context, Name))
                {
                    return new JsonResult("Sorry, this Name is already in use.");
                }
            }
            return new JsonResult(true);
        }
    }
}