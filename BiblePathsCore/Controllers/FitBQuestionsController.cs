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
    public class FitBQuestionController : ControllerBase
    {
        private readonly BiblePathsCoreDbContext _context;

        public FitBQuestionController(BiblePathsCoreDbContext context)
        {
            _context = context;
        }

         // GET: api/FitBQuestion
        [HttpGet]
        public async Task<ActionResult<MinQuestion>> GetFitBQuestion(string BibleId, string BookName, int Chapter, int Verse)
        {
            BibleId = await QuizQuestion.GetValidBibleIdAsync(_context, BibleId);

            BibleBook Book = await BibleBook.GetBookAndChapterByNameAsync(_context, BibleId, BookName, Chapter);
            if (Book == null) { return NotFound(); }
            BibleVerse verse = new BibleVerse();

            try
            {
                verse = await _context.BibleVerses.Where(v => v.BibleId == BibleId
                                                                && v.BookNumber == Book.BookNumber
                                                                && v.Chapter == Chapter
                                                                && v.Verse == Verse)
                                                            .SingleAsync();
            }
            catch
            {
                return NotFound();
            }

            QuizQuestion Question = new QuizQuestion();

            Question = await Question.BuildQuestionForVerseAsync(_context, verse, 8, BibleId);
            Question.PopulatePBEQuestionInfo(Book);
            MinQuestion minQuestion = new MinQuestion(Question);

            return minQuestion;
        }
    }
}
