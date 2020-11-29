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
    public class BiblesController : ControllerBase
    {
        private readonly BiblePathsCoreDbContext _context;

        public BiblesController(BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        // GET: api/Bibles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MinBible>>> GetBibles()
        {
            try
            {
                List<Bible> BibleList = await _context.Bibles.Include(b => b.BibleBooks).ToListAsync();
                List<MinBible> minBibles = new List<MinBible>();
                foreach (Bible bible in BibleList)
                {
                    _ = bible.HydrateBible();
                    MinBible minBible = new MinBible(bible);
                    minBibles.Add(minBible);
                }
                return minBibles;
            }
            catch { }
            return null;
        }

        // GET: api/Bibles/KJV-EN
        [HttpGet("{id}")]
        public async Task<ActionResult<MinBible>> GetBibles(string id)
        {
            var bible = await _context.Bibles.FindAsync(id);
            if (bible == null)
            {
                return NotFound();
            }
            await _context.Entry(bible).Collection(b => b.BibleBooks).LoadAsync();
            _ = bible.HydrateBible();
            MinBible minBible = new MinBible(bible);
            return minBible;
        }

        //// PUT: api/Bibles/5
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for
        //// more details see https://aka.ms/RazorPagesCRUD.
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutBibles(string id, Bibles bibles)
        //{
        //    if (id != bibles.Id)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(bibles).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!BiblesExists(id))
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

        // POST: api/Bibles
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        //[HttpPost]
        //public async Task<ActionResult<Bibles>> PostBibles(Bibles bibles)
        //{
        //    _context.Bibles.Add(bibles);
        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateException)
        //    {
        //        if (BiblesExists(bibles.Id))
        //        {
        //            return Conflict();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return CreatedAtAction("GetBibles", new { id = bibles.Id }, bibles);
        //}

        //// DELETE: api/Bibles/5
        //[HttpDelete("{id}")]
        //public async Task<ActionResult<Bibles>> DeleteBibles(string id)
        //{
        //    var bibles = await _context.Bibles.FindAsync(id);
        //    if (bibles == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.Bibles.Remove(bibles);
        //    await _context.SaveChangesAsync();

        //    return bibles;
        //}

        //private bool BiblesExists(string id)
        //{
        //    return _context.Bibles.Any(e => e.Id == id);
        //}
    }
}
