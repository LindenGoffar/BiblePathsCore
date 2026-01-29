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
using System.Text.Json;
using Microsoft.AspNetCore.Routing.Template;

namespace BiblePathsCore.Pages.PBE
{
    [Authorize] 
    public class CopyTemplateModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public CopyTemplateModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        [BindProperty]
        public PredefinedQuiz Template { get; set; }
        public List<MinBook> TemplateBooks { get; set; }
        public string JSONBooks { get; set; }
        [BindProperty]
        public List<PredefinedQuizQuestion> Questions { get; set; }
        [BindProperty] 
        public String BibleId { get; set; }
        public QuizUser PBEUser { get; set; }


        public async Task<IActionResult> OnGetAsync(int Id, string BibleId)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance

            Template = await _context.PredefinedQuizzes.FindAsync(Id);
            if (Template == null) { return RedirectToPage("/error", new { errorMessage = "Thats Odd! We were unable to find this Quiz Template." }); }
            if (Template.Type != (int)QuizTemplateType.Shared) { return RedirectToPage("/error", new { errorMessage = "The Quiz Template is not currently shared, we cannot allow you to make a copy of it." }); }

            if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to copy this Quiz Template" }); }

            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            //Initialize Our Template
            _context.Entry(Template).Collection(T => T.PredefinedQuizQuestions).Load();
            Questions = Template.InitQuestionListForAddEdit();

            // Initialize Template Books
            TemplateBooks = await Template.GetTemplateBooksAsync(_context, this.BibleId);
            JSONBooks = JsonSerializer.Serialize(TemplateBooks);

            // Build Select Lists
            foreach (PredefinedQuizQuestion Question in Questions)
            {
                Question.AddChapterSelectList(TemplateBooks);
            }
            ViewData["BookSelectList"] = MinBook.GetMinBookSelectListFromList(TemplateBooks);

            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int Id)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);

            PredefinedQuiz TemplateToCopy = await _context.PredefinedQuizzes.FindAsync(Id);
            if (TemplateToCopy == null) { return RedirectToPage("/error", new { errorMessage = "Thats Odd! We were unable to find the shared Quiz Template" }); }
            if (TemplateToCopy.Type != (int)QuizTemplateType.Shared) { return RedirectToPage("/error", new { errorMessage = "The Quiz Template is not currently shared, we cannot allow you to make a copy of it." }); }

            if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to copy this Quiz Template" }); }

            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            // We need a copy of the Questions list to compare to while the original is being updated.
            // List<PredefinedQuizQuestion> CompareQuestions = TemplateToCopy.PredefinedQuizQuestions.ToList();

            if (!ModelState.IsValid)
            {
                //Initialize Our Template Questions
                Questions = TemplateToCopy.InitQuestionListForAddEdit();
                // Initialize Template Books
                TemplateBooks = await TemplateToCopy.GetTemplateBooksAsync(_context, this.BibleId);
                JSONBooks = JsonSerializer.Serialize(TemplateBooks);
                // Build Select Lists
                foreach (PredefinedQuizQuestion Question in Questions)
                {
                    Question.AddChapterSelectList(TemplateBooks);
                }
                ViewData["BookSelectList"] = MinBook.GetMinBookSelectListFromList(TemplateBooks);

                return Page();
            }

            // Now let's create a new empty template
            var emptyTemplate = new PredefinedQuiz
            {
                Created = DateTime.Now,
                Modified = DateTime.Now,
                QuizUser = PBEUser

            };
            if (await TryUpdateModelAsync<PredefinedQuiz>(
                emptyTemplate,
                "Template",   // Prefix for form value.
                t => t.QuizName))
            {
                emptyTemplate.IsDeleted = false;
                emptyTemplate.BookNumber = TemplateToCopy.BookNumber;
                emptyTemplate.NumQuestions = TemplateToCopy.NumQuestions;
                emptyTemplate.Type = (int)QuizTemplateType.Standard;
                _context.PredefinedQuizzes.Add(emptyTemplate);
                await _context.SaveChangesAsync();
            }

            // Iterate through each of our Questions and make appropriate changes. 
            foreach (PredefinedQuizQuestion Question in Questions)
            {

                if (Question.BookNumber != 0)
                {
                    PredefinedQuizQuestion QuestionToAdd = new PredefinedQuizQuestion();
                    QuestionToAdd.PredefinedQuiz = emptyTemplate;
                    QuestionToAdd.QuestionNumber = Question.QuestionNumber;
                    QuestionToAdd.BookNumber = Question.BookNumber;
                    QuestionToAdd.Chapter = Question.Chapter;
                    _context.PredefinedQuizQuestions.Add(QuestionToAdd);
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Templates", new { Message = String.Format("Quiz Template {0} configured successfuly.", emptyTemplate.QuizName) });
        }
    }
}
