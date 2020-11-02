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
using Microsoft.AspNetCore.Authorization;
using static BiblePathsCore.Models.DB.QuizQuestions;

namespace BiblePathsCore.Pages.PBE
{
    [Authorize]
    public class QuizModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public QuizModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public QuizQuestions Question { get; set; }
        public QuizGroupStats Quiz { get; set; }
        public QuizUsers PBEUser { get; set; }
        public string BibleId { get; set; }
        public string UserMessage { get; set;  }

        public async Task<IActionResult> OnGetAsync(string BibleId, int QuizId, string Message)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUsers.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            this.BibleId = await Bibles.GetValidPBEBibleIdAsync(_context, BibleId);

            // Let's grab the Quiz Object
            Quiz = await _context.QuizGroupStats.FindAsync(QuizId);
            if (Quiz == null)
            {
                return RedirectToPage("/error", new { errorMessage = "That's Odd... We were unable to find this Quiz" });
            }
            if (Quiz.QuizUser != PBEUser) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Quiz Owner can run a Quiz" }); }
            _ = await Quiz.AddQuizPropertiesAsync(_context, BibleId);

            // Now for the Question Object... we're going to take 3 swings at this for the scenario where we don't have enough questions. 
            int iterations = 0;
            do
            {
                Question = await Quiz.GetNextQuizQuestionAsync(_context, BibleId);
                iterations++;
            }
            while (iterations < 3 && Question.QuestionSelected == false);

            // This is the no questions found scenario
            if (Question.QuestionSelected == false) { return RedirectToPage("/error", new { errorMessage = "Sorry! We failed to find a random question after three tries... please add more questions." }); }
            
            if (Question.BibleId == null) { Question.BibleId = BibleId;  }

            BibleBooks PBEBook = await BibleBooks.GetPBEBookAndChapterAsync(_context, BibleId, Question.BookNumber, Question.Chapter);
            if (PBEBook == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find the PBE Book." }); }

            Question.PopulatePBEQuestionInfo(PBEBook);
            Question.Verses = await Question.GetBibleVersesAsync(_context, true);
            Question.LegalNote = Question.GetBibleLegalNote();

            // Build our Select List and set a default points value of -1 to require selection.
            ViewData["PointsSelectList"] = Question.GetQuestionPointsSelectList();
            Question.PointsAwarded = -1;

            UserMessage = GetUserMessage(Message);
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(string BibleId, int QuizId)
        {
            if (!ModelState.IsValid)
            {
                // Something bad has happened let's go get a new quiz question.
                UserMessage = "Something has gone wrong! We were unable to save the results for that last question";
                return RedirectToPage("Quiz", new { BibleId, QuizId, Message = UserMessage });
            }

            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUsers.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            this.BibleId = await Bibles.GetValidPBEBibleIdAsync(_context, BibleId);

            // Let's grab the Quiz Object in order to update it. 
            Quiz = await _context.QuizGroupStats.FindAsync(QuizId);
            if (Quiz == null)
            {
                return RedirectToPage("/error", new { errorMessage = "That's Odd... We were unable to find this Quiz" });
            }
            if (Quiz.QuizUser != PBEUser) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Quiz Owner can award points during a Quiz" }); }

            // We need to upate the Question object as well so let's go grab it. 
            QuizQuestions QuestionToUpdate = await _context.QuizQuestions.FindAsync(Question.Id);
            if (QuestionToUpdate == null)
            {
                return RedirectToPage("/error", new { errorMessage = "That's Odd... We were unable to find this Question so we can't update it" });
            }

            // Now we award the points... let's get this right: 
            // Let's prevent posting an anomalous number of points. 
            int QuestionPointsPossible = QuestionToUpdate.Points;
            if (Question.PointsAwarded > QuestionPointsPossible)
            { Question.PointsAwarded = QuestionPointsPossible; }
            if (Question.PointsAwarded < 0) { Question.PointsAwarded = 0; }

            // Update the Quiz Object: 
            _context.Attach(Quiz);
            Quiz.PointsPossible += QuestionPointsPossible;
            Quiz.PointsAwarded += Question.PointsAwarded;
            Quiz.QuestionsAsked += 1;
            Quiz.Modified = DateTime.Now;

            // Update the Question Object
            _context.Attach(QuestionToUpdate);
            QuestionToUpdate.LastAsked = DateTime.Now;

            // Save both of these changes. 
            await _context.SaveChangesAsync();

            // And next let's make sure we log this event. 
            // BUG: Note we've had a pretty significant data bug prior to 6/8/2019 where we were setting Points to the cumulative quizGroupStat.PointsAwarded vs. the non-cumulative PointsAwardedByJudge... so all data prior to this date is wrong. 
            await QuestionToUpdate.RegisterEventAsync(_context, QuestionEventType.QuestionPointsAwarded, PBEUser.Id, null, Quiz.Id, Question.PointsAwarded);
            return RedirectToPage("Quiz", new { BibleId, QuizId });
        }

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
