using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Identity;

namespace BiblePathsCore
{
    public class DeleteTemplateModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public DeleteTemplateModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public PredefinedQuizzes Template { get; set; }
        public QuizUsers PBEUser { get; set; }

        public void OnGet(int? id)
        {
            RedirectToPage("/error", new { errorMessage = "That's Odd! This page should never be hit... " });

        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                Template = await _context.PredefinedQuizzes.Include(T => T.PredefinedQuizQuestions).Where(T => T.Id == id).SingleAsync();
            }
            catch {
                return RedirectToPage("/error", new
                {
                    errorMessage = "That's Odd! We were unable to retrieve this Template... Maybe try that again?"
                });
            }

            // confirm Template Owner
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUsers.GetOrAddPBEUserAsync(_context, user.Email);
            if (Template.QuizUser != PBEUser) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Template Owner may delete a Template" }); }

            // First we need to iterate through each Step and delete them one by one, steps are a leaf node so this should be OK.
            foreach (PredefinedQuizQuestions Question in Template.PredefinedQuizQuestions)
            {
                _context.PredefinedQuizQuestions.Remove(Question);
                await _context.SaveChangesAsync();
            }
            // Let's track this event 
            // _ = await Path.RegisterEventAsync(_context, EventType.PathDeleted, Path.Id.ToString());

            // Then we set the Template to isDeleted.
            if (Template != null)
            {
                _context.Attach(Template).State = EntityState.Modified;
                Template.Modified = DateTime.Now;
                Template.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Templates");
        }
    }
}
