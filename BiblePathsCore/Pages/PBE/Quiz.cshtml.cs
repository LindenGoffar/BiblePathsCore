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
using BiblePathsCore.Services;

namespace BiblePathsCore.Pages.PBE
{
    [Authorize]
    public class QuizModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;
        private readonly IOpenAIResponder _openAIResponder;

        public QuizModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context, IOpenAIResponder openAIResponder)
        {
            _userManager = userManager;
            _context = context;
            _openAIResponder = openAIResponder;
        }

        [BindProperty]
        public QuizQuestion Question { get; set; }
        public QuizGroupStat Quiz { get; set; }
        public QuizUser PBEUser { get; set; }
        public string BibleId { get; set; }
        public string UserMessage { get; set;  }

        public async Task<IActionResult> OnGetAsync(string BibleId, int QuizId, string Message)
        {
            // Get and validate our user.
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (!PBEUser.IsValidPBEQuizHost()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to host a PBE Quiz" }); }
            
            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            // Let's grab the Quiz Object
            Quiz = await _context.QuizGroupStats.FindAsync(QuizId);
            if (Quiz == null)
            {
                return RedirectToPage("/error", new { errorMessage = "That's Odd... We were unable to find this Quiz" });
            }
            if (Quiz.QuizUser != PBEUser) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Quiz Owner can run a Quiz" }); }
            
            _ = await Quiz.AddQuizPropertiesAsync(_context, BibleId);

            // Now for the Question Object... we're going to take 3 swings at this for the scenario where we don't have enough questions. 
            Question = await Quiz.GetOrBuildNextQuizQuestionAsync(_context, BibleId, _openAIResponder, PBEUser);
            if (Question.QuestionSelected == false)
            {
                return RedirectToPage("/error", new { errorMessage = "Sorry! We could neither find a question, nor generate one... please help by adding more questions." });
            }

            // no real good reason this wouldn't be set but out of an abundance of caution. 
            if (Question.BibleId == null) { Question.BibleId = BibleId;  }

            BibleBook PBEBook = await BibleBook.GetPBEBookAndChapterAsync(_context, BibleId, Question.BookNumber, Question.Chapter);
            if (PBEBook == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find the PBE Book." }); }

            // Note: the Commentary Scenario requires Verses be populated before PopulatePBEQuestionInfo is called. 
            Question.Verses = await Question.GetBibleVersesAsync(_context, true);
            Question.PopulatePBEQuestionInfo(PBEBook);

            // See if we have a TeamId, if so let's go chase down Members. 
            if (Quiz.QuizTeamId > 0)
            {
                _ = await Question.AddTeamMemberInfoAsync(_context, Quiz.QuizTeamId);
            }

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
            // Validate our User
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            if (!PBEUser.IsValidPBEQuizHost()) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to host a PBE Quiz" }); }

            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            // Let's grab the Quiz Object in order to update it. 
            Quiz = await _context.QuizGroupStats.FindAsync(QuizId);
            if (Quiz == null)
            {
                return RedirectToPage("/error", new { errorMessage = "That's Odd... We were unable to find this Quiz" });
            }
            if (Quiz.QuizUser != PBEUser) { return RedirectToPage("/error", new { errorMessage = "Sorry! Only a Quiz Owner can award points during a Quiz" }); }

            // We need to upate the Question object as well so let's go grab it. 
            QuizQuestion QuestionToUpdate = await _context.QuizQuestions.FindAsync(Question.Id);
            if (QuestionToUpdate == null)
            {
                return RedirectToPage("/error", new { errorMessage = "That's Odd... We were unable to find this Question so we can't update it" });
            }

            // If the question was AIProposed and still Challenged, let's removed the challenge before saving it. 
            if (QuestionToUpdate.Type == (int)QuestionType.AIProposed && QuestionToUpdate.Challenged == true)
            {
                QuestionToUpdate.Challenged = false;
                QuestionToUpdate.ChallengeComment = "Challenge Removed by user point assignment.";
            }

            // The following method adds the points to the quiz, updates the question with LastAsked, and updates Quiz Stats. 
            _ = await Quiz.AddQuizPointsforQuestionAsync(_context, QuestionToUpdate, Question.PointsAwarded, PBEUser);

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
