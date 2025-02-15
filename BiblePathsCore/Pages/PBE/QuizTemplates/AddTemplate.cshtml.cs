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
using System.Data;
using System.ComponentModel.DataAnnotations;

namespace BiblePathsCore.Pages.PBE
{
    [Authorize]
    public class AddTemplateModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public AddTemplateModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public PredefinedQuiz Template { get; set; }

        [BindProperty] 
        public String BibleId { get; set; }
        public QuizUser PBEUser { get; set; }

        //[PageRemote(
        //    ErrorMessage = "Sorry, this is not a valid Template Name",
        //    AdditionalFields = "__RequestVerificationToken, Template.QuizName",
        //    HttpMethod = "post",
        //    PageHandler = "CheckName"
        //)]
        [BindProperty]
        [Required]
        public string Name { get; set; }

        public async Task<IActionResult> OnGetAsync(string BibleId)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); 
            if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a PBE Quiz Template" }); }
          
            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            ViewData["BookSelectList"] = await BibleBook.GetBookAndBookListSelectListAsync(_context, BibleId);
            ViewData["CountSelectList"] = PredefinedQuiz.GetCountSelectList();
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            // Sanity check Step.Text
            ContentReview CheckThis = new ContentReview(Name);
            if (CheckThis.FindBannedWords() > 0)
            {
                ModelState.AddModelError("Name", "Please choose a Name that might be more inline with the mission of BiblePaths.");
            }
            if (!ModelState.IsValid)
            {
                ViewData["BookSelectList"] = await BibleBook.GetBookAndBookListSelectListAsync(_context, BibleId);
                ViewData["CountSelectList"] = PredefinedQuiz.GetCountSelectList();
                return Page();
            }

            // confirm our user is a valid PBE User. 
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);
            if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a PBE Quiz Template" }); }

            // Now let's create an empty template
            var emptyTemplate = new PredefinedQuiz
            {
                Created = DateTime.Now,
                Modified = DateTime.Now,
                QuizUser = PBEUser
                
            };
            if (await TryUpdateModelAsync<PredefinedQuiz>(
                emptyTemplate,
                "Template",   // Prefix for form value.
                t => t.QuizName, t => t.BookNumber, t => t.NumQuestions))
            {
                emptyTemplate.QuizName = Name; // Name is handled seperately for remote validation to work. 
                emptyTemplate.IsDeleted = false;
                _context.PredefinedQuizzes.Add(emptyTemplate);
                await _context.SaveChangesAsync();

                return RedirectToPage("./ConfigureTemplate", new { Id = emptyTemplate.Id, BibleId = this.BibleId });
            }

            return RedirectToPage("./Templates", new { Message = String.Format("Quiz Template {0} successfully created...", emptyTemplate.QuizName) });
        }
        //public async Task<JsonResult> OnPostCheckNameAsync()
        //{
        //    if (await Path.PathNameAlreadyExistsStaticAsync(_context, Name))
        //    {
        //        return new JsonResult("Sorry, this Name is already in use.");
        //    }
        //    return new JsonResult(true);
        //}
    }
}
