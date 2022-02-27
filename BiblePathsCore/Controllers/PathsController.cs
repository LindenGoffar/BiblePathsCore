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
    public class PathsController : ControllerBase
    {
        private readonly BiblePathsCoreDbContext _context;

        public PathsController(BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        // GET: api/Paths
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Path>>> GetPaths()
        {
            return await _context.Paths.Where(P => P.IsDeleted == false
                                                && P.Type == (int)PathType.Standard
                                                && P.IsPublished == true)
                                        .ToListAsync();
        }

        // GET: api/Paths/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Path>> GetPaths(int id)
        {
            var paths = await _context.Paths.FindAsync(id);

            if (paths == null)
            {
                return NotFound();
            }

            return paths;
        }

        //// PUT: api/Paths/5
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for
        //// more details see https://aka.ms/RazorPagesCRUD.
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutPaths(int id, Paths paths)
        //{
        //    if (id != paths.Id)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(paths).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!PathsExists(id))
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

        //// POST: api/Paths
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for
        //// more details see https://aka.ms/RazorPagesCRUD.
        //[HttpPost]
        //public async Task<ActionResult<Paths>> PostPaths(Paths paths)
        //{
        //    _context.Paths.Add(paths);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetPaths", new { id = paths.Id }, paths);
        //}

        //// DELETE: api/Paths/5
        //[HttpDelete("{id}")]
        //public async Task<ActionResult<Paths>> DeletePaths(int id)
        //{
        //    var paths = await _context.Paths.FindAsync(id);
        //    if (paths == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.Paths.Remove(paths);
        //    await _context.SaveChangesAsync();

        //    return paths;
        //}

        private bool PathsExists(int id)
        {
            return _context.Paths.Any(e => e.Id == id);
        }
    }
}
