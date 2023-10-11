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

namespace BiblePathsCore.Pages.PBE
{
    [Authorize]
    public class AddQuestionModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public AddQuestionModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        [BindProperty]
        public QuizQuestion Question { get; set; }
        [BindProperty]
        public string AnswerText { get; set; }
        public QuizUser PBEUser { get; set; }
        public bool IsCommentary { get; set; }
        public bool HasExclusion { get; set; }
        public int CommentaryQuestionCount { get; set; }
        public int ChapterQuestionCount { get; set; }

        public async Task<IActionResult> OnGetAsync(string BibleId, int BookNumber, int Chapter, int? VerseNum, bool? BuildQuestion)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a PBE question" }); }

            bool generateQuestion = false;
            if (BuildQuestion.HasValue)
            {
                generateQuestion = (bool)BuildQuestion;
            }
            Question = new QuizQuestion();
            // Setup our PBEBook Object
            Question.BibleId = await QuizQuestion.GetValidBibleIdAsync(_context, BibleId);

            if (generateQuestion)
            {
                BibleVerse verse = await BibleVerse.GetVerseAsync(_context, Question.BibleId, BookNumber, Chapter, (int)VerseNum);
                Question = await Question.BuildQuestionForVerseAsync(_context, verse, 10, Question.BibleId);
                foreach (QuizAnswer Answer in Question.QuizAnswers)
                {
                    AnswerText += Answer.Answer;
                }
            }
            else
            {
                Question.BookNumber = BookNumber;
                Question.Chapter = Chapter;
                Question.StartVerse = VerseNum ?? 1; // set to 1 if VersNum is Null.
                Question.EndVerse = VerseNum ?? 1; // set to 1 if VersNum is Null.
                Question.Points = 0;
            }

            BibleBook PBEBook = await BibleBook.GetPBEBookAndChapterAsync(_context, Question.BibleId, Question.BookNumber, Question.Chapter);
            if (PBEBook == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find the PBE Book." }); }

            Question.PopulatePBEQuestionInfo(PBEBook);
            Question.Verses = await Question.GetBibleVersesAsync(_context, false);

            HasExclusion = Question.Verses.Any(v => v.IsPBEExcluded == true);

            IsCommentary = (Question.Chapter == Bible.CommentaryChapter);
            if (IsCommentary == false)
            {
                ChapterQuestionCount = PBEBook.BibleChapters.Where(c => c.ChapterNumber == Question.Chapter).First().QuestionCount;
            } 
            CommentaryQuestionCount = PBEBook.CommentaryQuestionCount;

            
            // and now we need a Verse Select List
            ViewData["VerseSelectList"] = new SelectList(Question.Verses, "Verse", "Verse");
            ViewData["PointsSelectList"] = Question.GetPointsSelectList();
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Setup our PBEBible Object
                Question.BibleId = await QuizQuestion.GetValidBibleIdAsync(_context, Question.BibleId);
                BibleBook PBEBook = await BibleBook.GetPBEBookAndChapterAsync(_context, Question.BibleId, Question.BookNumber, Question.Chapter);
                if (PBEBook == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find the PBE Book." }); }

                Question.PopulatePBEQuestionInfo(PBEBook);
                Question.Verses = await Question.GetBibleVersesAsync(_context, false);

                HasExclusion = Question.Verses.Any(v => v.IsPBEExcluded == true);

                IsCommentary = (Question.Chapter == Bible.CommentaryChapter);
                if (IsCommentary == false)
                {
                    ChapterQuestionCount = PBEBook.BibleChapters.Where(c => c.ChapterNumber == Question.Chapter).First().QuestionCount;
                }
                CommentaryQuestionCount = PBEBook.CommentaryQuestionCount;

                // and now we need a Verse and Points Select List
                ViewData["VerseSelectList"] = new SelectList(Question.Verses, "Verse", "Verse");
                ViewData["PointsSelectList"] = Question.GetPointsSelectList();
                return Page();
            }

            // confirm our user is a valid PBE User. 
            IdentityUser user = await _userManager.GetUserAsync(User);
            if (User != null)
            {
                PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);
                if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add a PBE question" }); }
            }
            else { return RedirectToPage("/error", new { errorMessage = "Oops! We were unable to get our User Object from the UserManager, this question cannot be added!" }); }

            // Now let's create an empty question and put only our validated properties onto it. 
            var emptyQuestion = new QuizQuestion
            {
                Created = DateTime.Now,
                Modified = DateTime.Now
            };

            if (await TryUpdateModelAsync<QuizQuestion>(
                emptyQuestion,
                "Question",   // Prefix for form value.
                Q => Q.BibleId, Q => Q.Points, Q => Q.BookNumber, Q => Q.Chapter, Q => Q.StartVerse, Q => Q.EndVerse, Q => Q.Question))
            {
                // If the Question is in an Exclusion range we will show an Error
                if (await emptyQuestion.IsQuestionInExclusionAsync(_context)) { return RedirectToPage("/error", new { errorMessage = "Sorry! One of the verses associated with this question is curently excluded from PBE Testing." }); }

                emptyQuestion.Owner = PBEUser.Email;
                emptyQuestion.Source = "BiblePaths.Net";
                emptyQuestion.Type = (int)QuestionType.Standard;
                _context.QuizQuestions.Add(emptyQuestion);

                // now we need to add the Answer if there is one. 
                if (AnswerText.Length > 0) 
                {
                    QuizAnswer Answer = new QuizAnswer
                    {
                        Created = DateTime.Now,
                        Modified = DateTime.Now,
                        Question = emptyQuestion,
                        Answer = AnswerText,
                        IsPrimary = true
                    };
                    _context.QuizAnswers.Add(Answer);
                    // Register that this question has an answer. 
                    emptyQuestion.IsAnswered = true;
                }
                await _context.SaveChangesAsync();

                return RedirectToPage("AddQuestion", new { BibleId = emptyQuestion.BibleId, BookNumber = emptyQuestion.BookNumber, Chapter = emptyQuestion.Chapter, VerseNum = emptyQuestion.EndVerse });
            }
            else { return RedirectToPage("/error", new { errorMessage = "Oops! We failed to update the question model, this question cannot be added!" }); }
            // return RedirectToPage("Index");
        }
    }
}
