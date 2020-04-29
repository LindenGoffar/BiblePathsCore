using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Http;

namespace BiblePathsCore
{
    public class PathCompletedModel : PageModel
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public PathCompletedModel(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }
        public Paths Path { get; set; }
        public bool RatingAccepted { get; set; }
        public bool RatingAcknowledged { get; set; }
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Path = await _context.Paths.FindAsync(id);
            if (Path == null)
            {
                return NotFound();
            }
            _ = await Path.AddCalculatedPropertiesAsync(_context);

            // We have a 15 min back off timer on rating a given path, to try and avoid rating abuse
            // To calculate this we need to find time of the the latest userRating
            RatingAccepted = false;
            DateTimeOffset? LatestUserRating = null;
            try
            {
                LatestUserRating = (await _context.PathStats
                                    .Where(s => s.PathId == Path.Id && s.EventType == (int)EventType.UserRating)
                                    .OrderByDescending(s => s.EventWritten)
                                    .FirstAsync()).EventWritten;
            }
            catch
            {
                // we assume this Path is not rated yet so let's accept a rating. 
                RatingAccepted = true; 
            }
            if (LatestUserRating.HasValue)
            {
                TimeSpan TimeSinceLastRated = (TimeSpan)(DateTime.Now - LatestUserRating);
                if (TimeSinceLastRated.TotalMinutes > 15)
                {
                    RatingAccepted = true;
                }
            }

            RatingAcknowledged = false;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id, int? rating)
        {
            if (id == null)
            {
                return NotFound();
            }
            Path = await _context.Paths.FindAsync(id);
            if (Path == null)
            {
                return NotFound();
            }
            _ = await Path.AddCalculatedPropertiesAsync(_context);

            if (rating.HasValue)
            {
                if (rating > 0 && rating <= 5)
                {
                    _ = await Path.RegisterEventAsync(_context, EventType.UserRating, rating.ToString());
                }
            }
            // Now let's Apply a new rating to this Path 
            _ = await Path.ApplyPathRatingAsyc(_context);
            RatingAcknowledged = true;
            RatingAccepted = false; 
            return Page();
        }
    }
}
