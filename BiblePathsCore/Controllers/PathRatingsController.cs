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
    public class PathRatingsController : ControllerBase
    {
        private readonly BiblePathsCoreDbContext _context;

        public PathRatingsController(BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        // GET: api/PathRatings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Double>> GetPathRatings(int id)
        {
            Double returnValue = 0.0;
            var path = await _context.Paths.FindAsync(id);

            if (path == null)
            {
                return NotFound();
            }

            else
            {
                if (await path.ApplyPathRatingAsync(_context)) { returnValue = (Double)path.ComputedRating; }
            }
            return returnValue;
        }
    }
}
