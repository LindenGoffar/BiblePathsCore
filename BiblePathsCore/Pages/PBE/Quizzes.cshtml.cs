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
    public class QuizzesModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public QuizzesModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        //public List<PredefinedQuiz> Templates { get;set; }
        //public List<PredefinedQuiz> SharedTemplates { get; set; }
        //public List<QuizBookList> BookLists { get; set; }
        public List<QuizGroupStat> Quizzes { get; set; }
        public QuizUser PBEUser { get; set; }
        public string BibleId { get; set; }
        public string UserMessage { get; set;  }
        public string InitialTab { get; set; }
        
        //TODO: Tab targeting is not working this requires work on Shared _QuizzesTabHelper. 
        public async Task<IActionResult> OnGetAsync(string BibleId, string Message, string Tab = "Quizzes")
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);


            Quizzes = await _context.QuizGroupStats.Where(G => G.QuizUser == PBEUser
                                                           && G.IsDeleted == false)
                                                   .OrderByDescending(Q => Q.Modified)
                                                   .ToListAsync();

            //TODO: Tab targeting is not working this requires work on Shared _QuizzesTabHelper. 
            InitialTab = Tab;
            // Populate Quiz Info 
            foreach (QuizGroupStat quiz in Quizzes)
            {
                _ = await quiz.AddQuizPropertiesAsync(_context, this.BibleId);
            }

            UserMessage = GetUserMessage(Message);
            return Page();
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
