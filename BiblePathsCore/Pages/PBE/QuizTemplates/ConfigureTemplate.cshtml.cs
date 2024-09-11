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
    public class ConfigureTemplateModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public ConfigureTemplateModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
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

        [BindProperty]
        public bool isShared { get; set; }
        public QuizUser PBEUser { get; set; }


        public async Task<IActionResult> OnGetAsync(int Id, string BibleId)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance

            Template = await _context.PredefinedQuizzes.FindAsync(Id);
            if (Template == null) { return RedirectToPage("/error", new { errorMessage = "Thats Odd! We were unable to find this Quiz Template" }); }

            if (!PBEUser.IsValidPBEQuestionBuilder() || PBEUser != Template.QuizUser) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to configure this Quiz Template" }); }
          
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

            if (Template.Type == (int)QuizTemplateType.Shared) { isShared = true; }
            else { isShared = false; }

            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int Id)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);

            PredefinedQuiz TemplateToUpdate = await _context.PredefinedQuizzes.FindAsync(Id);
            if (TemplateToUpdate == null) { return RedirectToPage("/error", new { errorMessage = "Thats Odd! We were unable to find this Quiz Template" }); }

            if (!PBEUser.IsValidPBEQuestionBuilder() || PBEUser != TemplateToUpdate.QuizUser) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to configure this Quiz Template" }); }
            if (TemplateToUpdate.QuizUser != PBEUser) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Template Owner may edit a Template" }); }

            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            _context.Entry(TemplateToUpdate).Collection(T => T.PredefinedQuizQuestions).Load();

            // We need a copy of the Questions list to compare to while the original is being updated.
            List<PredefinedQuizQuestion> CompareQuestions = TemplateToUpdate.PredefinedQuizQuestions.ToList();

            if (!ModelState.IsValid)
            {
                //Initialize Our Template Questions
                Questions = TemplateToUpdate.InitQuestionListForAddEdit();
                // Initialize Template Books
                TemplateBooks = await TemplateToUpdate.GetTemplateBooksAsync(_context, this.BibleId);
                JSONBooks = JsonSerializer.Serialize(TemplateBooks);
                // Build Select Lists
                foreach (PredefinedQuizQuestion Question in Questions)
                {
                    Question.AddChapterSelectList(TemplateBooks);
                }
                ViewData["BookSelectList"] = MinBook.GetMinBookSelectListFromList(TemplateBooks);

                if (Template.Type == (int)QuizTemplateType.Shared) { isShared = true; }
                else { isShared = false; }

                return Page();
            }

            // First let's update our Template
            _context.Attach(TemplateToUpdate);
            TemplateToUpdate.Modified = DateTime.Now;
            TemplateToUpdate.QuizName = Template.QuizName;
            if (TemplateToUpdate.Type != (int)QuizTemplateType.Shared && isShared) { 
                TemplateToUpdate.Type = (int)QuizTemplateType.Shared; 
            }
            await _context.SaveChangesAsync();

            // Iterate through each of our Questions and make appropriate changes. 
            foreach (PredefinedQuizQuestion Question in Questions)
            {
                // See if this is one of our existing Question objects. 
                bool ExistingQuestion = true;
                PredefinedQuizQuestion OriginalQuestion = new PredefinedQuizQuestion();
                try
                {
                    OriginalQuestion = CompareQuestions.Where(Q => Q.QuestionNumber == Question.QuestionNumber).Single();
                }
                catch
                {
                    ExistingQuestion = false;
                    // New Question Scenario let's add the Question.
                    if (Question.BookNumber != 0)
                    {
                        PredefinedQuizQuestion QuestionToAdd = new PredefinedQuizQuestion();
                        QuestionToAdd.PredefinedQuiz = TemplateToUpdate;
                        QuestionToAdd.QuestionNumber = Question.QuestionNumber;
                        QuestionToAdd.BookNumber = Question.BookNumber;
                        QuestionToAdd.Chapter = Question.Chapter;
                        _context.PredefinedQuizQuestions.Add(QuestionToAdd);
                    }
                }
                if (ExistingQuestion)
                {
                    // Do we need to update OriginalQuestion? 
                    if (OriginalQuestion.BookNumber != Question.BookNumber
                        || OriginalQuestion.Chapter != Question.Chapter)
                    {
                        if (Question.BookNumber != 0)
                        {
                            // this is the update scenario
                            _context.Attach(OriginalQuestion);
                            OriginalQuestion.BookNumber = Question.BookNumber;
                            OriginalQuestion.Chapter = Question.Chapter;
                        }
                        else
                        {
                            // this is the delete scenario
                            _context.PredefinedQuizQuestions.Remove(OriginalQuestion);
                        }
                    }
                }
                await _context.SaveChangesAsync();

            }
            return RedirectToPage("./Quizzes", "", new { BibleId = this.BibleId, Message = String.Format("Quiz Template {0} configured successfuly.", TemplateToUpdate.QuizName) }, "Templates");
        }
    }
}
