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

namespace BiblePathsCore.Pages.Play
{
    [Authorize]
    public class GroupModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public GroupModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public GameGroup Group { get; set; }

        public Path SelectedPath { get; set; }
        public string UserMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int Id, string Message)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);

            Group = await _context.GameGroups.FindAsync(Id);
            if (Group == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find that Group" }); }
            if (Group.Owner != user.Email) { return RedirectToPage("/error", new { errorMessage = "Sorry, Only the owner can manage a Group" }); }
            SelectedPath = await _context.Paths.FindAsync(Group.PathId);
            if (SelectedPath == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find the Path for this Group" }); }

            _context.Entry(Group)
            .Collection(g => g.GameTeams)
            .Load();

            ViewData["PathSelectList"] = await GameGroup.GetPathSelectListAsync(_context);
            UserMessage = GetUserMessage(Message);
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        //public async Task<IActionResult> OnPostAsync()
        //{
            //if (!ModelState.IsValid)
            //{
            //    ViewData["BookSelectList"] = await BibleBooks.GetBookAndBookListSelectListAsync(_context, BibleId);
            //    return Page();
            //}

            //// confirm our user is a valid PBE User. 
            //IdentityUser user = await _userManager.GetUserAsync(User);
            //PBEUser = await QuizUsers.GetOrAddPBEUserAsync(_context, user.Email);
            //if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a PBE Quiz Template" }); }

            //// Now let's create an empty template
            //var emptyTemplate = new PredefinedQuizzes
            //{
            //    Created = DateTime.Now,
            //    Modified = DateTime.Now,
            //    QuizUser = PBEUser

            //};
            //if (await TryUpdateModelAsync<PredefinedQuizzes>(
            //    emptyTemplate,
            //    "Template",   // Prefix for form value.
            //    t => t.QuizName, t => t.BookNumber, t => t.NumQuestions))
            //{
            //    emptyTemplate.IsDeleted = false;
            //    _context.PredefinedQuizzes.Add(emptyTemplate);
            //    await _context.SaveChangesAsync();

            //    return RedirectToPage("./ConfigureTemplate", new { Id = emptyTemplate.Id, BibleId = this.BibleId });
            //}

        //    return Page();
        //}

        public string GetUserMessage(string Message)
        {
            if (Message != null)
            {
                // Arbitrarily limiting User Message length. 
                if (Message.Length > 0 && Message.Length < 128)
                {
                    return Message;
                }
            }
            return null;
        }

    }
}
