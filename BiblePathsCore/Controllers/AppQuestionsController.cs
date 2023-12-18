using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;

namespace BiblePathsCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppQuestionsController : ControllerBase
    {
        private readonly BiblePathsCoreDbContext _context;

        public AppQuestionsController(BiblePathsCoreDbContext context)
        {
            _context = context;
        }
        // GET: api/AppQuestions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MinQuestion>>> GetAppQuestions(string BibleId, string BookName, int Chapter, int Count = 2)
        {
            // Provided a Book, Chapter, and desired count, we will return questions 
            // Based on our standard PBE Quiz App Logic. 
            BibleId = await QuizQuestion.GetValidBibleIdAsync(_context, BibleId);

            BibleBook Book = await BibleBook.GetBookAndChapterByNameAsync(_context, BibleId, BookName, Chapter);
            if (Book == null) { return NotFound(); }

            // We need a QuizGroupStats object to reference
            QuizGroupStat TempQuiz = new QuizGroupStat();

            List<MinQuestion> minQuestions = new List<MinQuestion>();

            // We limit the max questions to 6 per chapter at a time. 
            if (Count > 6) { Count = 6; }
            if (Count < 1) { Count = 1; }
            // Go grab Count questions, and add them to our list. 
            // Also mark them as asked. 
            for (int i = 1; i <= Count; i++)
            {
                QuizQuestion Question = await TempQuiz.GetNextQuizQuestionFromBookAndChapterAsync(_context, BibleId, Book.BookNumber, Chapter);
                Question.PopulatePBEQuestionInfo(Book);
                MinQuestion minQuestion = new MinQuestion(Question);
                minQuestions.Add(minQuestion);
                _ = await Question.UpdateLastAskedAsync(_context);
            }
            return minQuestions;
        }
    }
}
