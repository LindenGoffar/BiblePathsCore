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
    public class EditQuestionModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public EditQuestionModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
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

        [BindProperty]
        public string ReturnPath { get; set; }

        public async Task<IActionResult> OnGetAsync(int QuestionId, string Caller)
        {
            ReturnPath = Caller;
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); 
            if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to edit a PBE question" }); }

            Question = await _context.QuizQuestions.FindAsync(QuestionId);
            if (Question == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Question" }); }

            // Setup our PBEBook Object
            Question.BibleId = await QuizQuestion.GetValidBibleIdAsync(_context, Question.BibleId);

            BibleBook PBEBook = await BibleBook.GetPBEBookAndChapterAsync(_context, Question.BibleId, Question.BookNumber, Question.Chapter);
            if (PBEBook == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find the PBE Book." }); }

            Question.PopulatePBEQuestionInfo(PBEBook);
            Question.Verses = await Question.GetBibleVersesAsync(_context, false);

            HasExclusion = Question.Verses.Any(v => v.IsPBEExcluded == true);

            // We need an answer text, and while techincally we support multiple Answers
            // we are only going to allow operating on the first one in this basic edit experience.
            await _context.Entry(Question).Collection(Q => Q.QuizAnswers).LoadAsync();
            if (Question.QuizAnswers.Count > 0)
            {
                AnswerText = Question.QuizAnswers.OrderBy(A => A.Id).First().Answer;
            }
            else { AnswerText = "";  }

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
                // We should still have AnswerText

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
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email);
            if (!PBEUser.IsValidPBEQuestionBuilder()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to edit a PBE question" }); }

            // Now let's create an empty question and put only our validated properties onto it. 
            QuizQuestion QuestionToUpdate = await _context.QuizQuestions.FindAsync(Question.Id);
            if (QuestionToUpdate == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Question" }); }

            if (await TryUpdateModelAsync<QuizQuestion>(
                QuestionToUpdate,
                "Question",   // Prefix for form value.
                Q => Q.BibleId, Q => Q.Points, Q => Q.StartVerse, Q => Q.EndVerse, Q => Q.Question, Q => Q.Challenged, Q => Q.ChallengeComment))
            {
                QuestionToUpdate.Modified = DateTime.Now;

                // If the Question is in an Exclusion range we will show an Error
                if (await QuestionToUpdate.IsQuestionInExclusionAsync(_context)) { return RedirectToPage("/error", new { errorMessage = "Sorry! One of the verses associated with this question is curently excluded from PBE Testing." }); }

                // now we need to add the Answer if there is one. 
                if (AnswerText.Length > 0) 
                {
                    // We need the Original Answer and while techincally we support multiple Answers
                    // we are only going to allow operating on the first one in this basic edit experience.
                    await _context.Entry(QuestionToUpdate).Collection(Q => Q.QuizAnswers).LoadAsync();
                    if (QuestionToUpdate.QuizAnswers.Count > 0)
                    {
                        QuizAnswer OriginalAnswer = QuestionToUpdate.QuizAnswers.OrderBy(A => A.Id).First();
                        if (OriginalAnswer.Answer != AnswerText)
                        {
                            _context.Attach(OriginalAnswer);
                            OriginalAnswer.Modified = DateTime.Now;
                            OriginalAnswer.Answer = AnswerText;

                            QuestionToUpdate.IsAnswered = true;
                        }
                    }
                }
                await _context.SaveChangesAsync();

                switch (ReturnPath)
                {
                    case "Questions":
                        return RedirectToPage("Questions", new { BibleId = QuestionToUpdate.BibleId, BookNumber = QuestionToUpdate.BookNumber, Chapter = QuestionToUpdate.Chapter});
                        // break; not needed unreachable

                    case "ChallengedQuestions":
                        return RedirectToPage("ChallengedQuestions", new { BibleId = QuestionToUpdate.BibleId, BookNumber = QuestionToUpdate.BookNumber, Chapter = QuestionToUpdate.Chapter});
                        // break; not needed unreachable

                    default:
                        return RedirectToPage("AddQuestion", new { BibleId = QuestionToUpdate.BibleId, BookNumber = QuestionToUpdate.BookNumber, Chapter = QuestionToUpdate.Chapter, VerseNum = QuestionToUpdate.EndVerse });
                        // break; not needed unreachable
                }

            }
            return RedirectToPage("Index");
        }
    }
}
