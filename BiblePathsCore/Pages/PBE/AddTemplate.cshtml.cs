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
using System.Data;

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
        public PredefinedQuizzes Template { get; set; }

        [BindProperty] 
        public String BibleId { get; set; }
        public QuizUsers PBEUser { get; set; }

        public async Task<IActionResult> OnGetAsync(string BibleId)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUsers.GetOrAddPBEUserAsync(_context, user.Email); 
            if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a PBE Quiz Template" }); }
          
            this.BibleId = await Bibles.GetValidPBEBibleIdAsync(_context, BibleId);

            ViewData["BookSelectList"] = await BibleBooks.GetBookAndBookListSelectListAsync(_context, BibleId);
            ViewData["CountSelectList"] = PredefinedQuizzes.GetCountSelectList();
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["BookSelectList"] = await BibleBooks.GetBookAndBookListSelectListAsync(_context, BibleId);
                return Page();
            }

            // confirm our user is a valid PBE User. 
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUsers.GetOrAddPBEUserAsync(_context, user.Email);
            if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a PBE Quiz Template" }); }

            // Now let's create an empty template
            var emptyTemplate = new PredefinedQuizzes
            {
                Created = DateTime.Now,
                Modified = DateTime.Now,
                QuizUser = PBEUser
                
            };
            if (await TryUpdateModelAsync<PredefinedQuizzes>(
                emptyTemplate,
                "Template",   // Prefix for form value.
                t => t.QuizName, t => t.BookNumber, t => t.NumQuestions))
            {
                emptyTemplate.IsDeleted = false;
                _context.PredefinedQuizzes.Add(emptyTemplate);
                await _context.SaveChangesAsync();

                return RedirectToPage("./ConfigureTemplate", new { Id = emptyTemplate.Id, BibleId = this.BibleId });
            }

            return RedirectToPage("./Templates", new { Message = String.Format("Quiz Template {0} successfully created...", emptyTemplate.QuizName) });
        }

    }
}
