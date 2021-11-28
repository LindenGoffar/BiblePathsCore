using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;

namespace BiblePathsCore.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class BibleChaptersController : ControllerBase
    {
        private readonly BiblePathsCoreDbContext _context;

        public BibleChaptersController(BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        // GET: api/BibleChapters
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MinChapter>>> GetBibleChapters(string BibleId, string BookName)
        {

            BibleId = await QuizQuestion.GetValidBibleIdAsync(_context, BibleId);

            BibleBook Book = await BibleBook.GetBookByNameAsync(_context, BibleId, BookName);
            if (Book == null) { return NotFound(); }

            try
            {
                List<BibleChapter> ChapterList = Book.BibleChapters.ToList();
                List<MinChapter> MinChapters = new List<MinChapter>();
                foreach (BibleChapter Chapter in ChapterList)
                {
                    MinChapter MinChapter = new MinChapter(Chapter);
                    MinChapters.Add(MinChapter);
                }
                return MinChapters;
            }
            catch { }
            return null;
        }
    }
}
