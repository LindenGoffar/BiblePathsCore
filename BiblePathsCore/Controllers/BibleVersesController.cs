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
    public class BibleVersesController : ControllerBase
    {
        private readonly BiblePathsCoreDbContext _context;

        public BibleVersesController(BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        //// GET: api/BibleVerses
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<BibleVerses>>> GetBibleVerses()
        //{
        //    return await _context.BibleVerses.ToListAsync();
        //}

        // GET: api/BibleVerses/?BibleID=$BibleID&BookNumber=$BookNumber&Chapter=$Chapter&StartVerse=$StartVerse&EndVerse=$EndVerse"
        [HttpGet]
        public async Task<List<BibleVerses>> GetBibleVerses(string BibleId, int BookNumber, int Chapter, int StartVerse, int EndVerse)
        {
            return await _context.BibleVerses.Where(v => v.BibleId == BibleId
                                                                && v.BookNumber == BookNumber
                                                                && v.Chapter == Chapter
                                                                && v.Verse >= StartVerse
                                                                && v.Verse <= EndVerse).ToListAsync();
        }

        //// PUT: api/BibleVerses/5
        //// To protect from overposting attacks, enable the specific properties you want to bind to, for
        //// more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutBibleVerses(int id, BibleVerses bibleVerses)
        //{
        //    if (id != bibleVerses.Id)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(bibleVerses).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!BibleVersesExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return NoContent();
        //}

        //// POST: api/BibleVerses
        //// To protect from overposting attacks, enable the specific properties you want to bind to, for
        //// more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        //[HttpPost]
        //public async Task<ActionResult<BibleVerses>> PostBibleVerses(BibleVerses bibleVerses)
        //{
        //    _context.BibleVerses.Add(bibleVerses);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetBibleVerses", new { id = bibleVerses.Id }, bibleVerses);
        //}

        //// DELETE: api/BibleVerses/5
        //[HttpDelete("{id}")]
        //public async Task<ActionResult<BibleVerses>> DeleteBibleVerses(int id)
        //{
        //    var bibleVerses = await _context.BibleVerses.FindAsync(id);
        //    if (bibleVerses == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.BibleVerses.Remove(bibleVerses);
        //    await _context.SaveChangesAsync();

        //    return bibleVerses;
        //}

        private bool BibleVersesExists(int id)
        {
            return _context.BibleVerses.Any(e => e.Id == id);
        }
    }
}
