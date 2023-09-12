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

namespace BiblePathsCore.Pages.PBE
{
    [Authorize]
    public class ChallengedQuestionsModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public ChallengedQuestionsModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IList<QuizQuestion> Questions { get;set; }
        public QuizUser PBEUser { get; set; }
        public int BookNumber { get; set; }
        public string BookName { get; set; }
        public int Chapter { get; set; }
        public string BibleId { get; set; }
        public bool IsCommentary { get; set; }
        public async Task<IActionResult> OnGetAsync(string BibleId, int BookNumber, int Chapter)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            this.BookNumber = BookNumber;
            this.Chapter = Chapter;
            this.BibleId = await QuizQuestion.GetValidBibleIdAsync(_context, BibleId);

            BibleBook PBEBook = await BibleBook.GetPBEBookAndChapterAsync(_context, this.BibleId, this.BookNumber, this.Chapter);
            if (PBEBook == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find the PBE Book." }); }

            Questions = await _context.QuizQuestions.Where(Q => (Q.BibleId == this.BibleId || Q.BibleId == null) 
                                                            && Q.BookNumber == BookNumber 
                                                            && Q.Chapter == Chapter 
                                                            && Q.Challenged == true 
                                                            && Q.IsDeleted == false
                                                            && Q.Type == (int)QuestionType.Standard)
                                        .Include(Q => Q.QuizAnswers)
                                        .OrderBy(Q => Q.EndVerse)
                                        .ToListAsync();

            foreach (QuizQuestion Question in Questions)
            {
                //_ = await Question.PopulatePBEQuestionAndBookInfoAsync(_context);
                Question.PopulatePBEQuestionInfo(PBEBook);
                Question.CheckUserCanEdit(PBEUser);
            }
            IsCommentary = (this.Chapter == Bible.CommentaryChapter);
            if (IsCommentary) { this.BookName = PBEBook.CommentaryTitle; }
            else { this.BookName = PBEBook.Name; }

            return Page();
        }
    }
}
