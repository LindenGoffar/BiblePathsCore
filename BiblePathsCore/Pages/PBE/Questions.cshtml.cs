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
    public class QuestionsModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public QuestionsModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IList<QuizQuestion> Questions { get;set; }
        public QuizUser PBEUser { get; set; }
        public int BookNumber { get; set; }
        public string BookName { get; set; }
        public int Chapter { get; set; }
        public int Verse { get; set;  }
        public string BibleId { get; set; }
        public bool IsCommentary { get; set; }

        public async Task<IActionResult> OnGetAsync(string BibleId, int BookNumber, int Chapter, int? Verse)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            this.BookNumber = BookNumber;
            this.Chapter = Chapter;
            this.Verse = Verse ?? 0; // set to 0 if Verse is null
            this.BibleId = await QuizQuestion.GetValidBibleIdAsync(_context, BibleId);

            BibleBook PBEBook = await BibleBook.GetPBEBookAndChapterAsync(_context, this.BibleId, this.BookNumber, this.Chapter);
            if (PBEBook == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find the PBE Book." }); }

            if (!Verse.HasValue)
            {
                Questions = await QuizQuestion.GetQuestionListAsync(_context, this.BibleId, BookNumber, Chapter, true);
            }
            else
            {
                Questions = await QuizQuestion.GetQuestionListAsync(_context, this.BibleId, BookNumber, Chapter, (int)Verse, true);
            }

            Questions = Questions.OrderBy(Q => Q.EndVerse).ToList(); 

            foreach (QuizQuestion Question in Questions)
            {
                Question.PopulatePBEQuestionInfo(PBEBook);
                Question.CheckUserCanEdit(PBEUser);
                // TODO: This may become temporary if expensive, but is useful for cleanup tasks. 
                // First see if maint may be needed. 
                if (Question.HasQuestionMaintenanceIssue())
                {
                    _ = await Question.PerformQuestionMaintenanceTasksAsync(_context);
                }
            }
            IsCommentary = (this.Chapter == Bible.CommentaryChapter);
            if (IsCommentary) { this.BookName = PBEBook.CommentaryTitle;  }
            else { this.BookName = PBEBook.Name;  }            

            return Page();
        }
    }
}
