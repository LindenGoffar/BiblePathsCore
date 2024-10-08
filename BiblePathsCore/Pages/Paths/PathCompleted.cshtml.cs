﻿using System;
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
        public Path Path { get; set; }
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

            // WE SHOULD NOT BE COMPLETING a Commented Path in this way so redirect... 
            // If the requested Path is a Commented Path then we need to redirected to the CommentedPaths reading experience
            if (Path.Type == (int)PathType.Commented) { return RedirectToPage("/CommentedPaths/Read", new { PathId = Path.Id }); }

            _ = await Path.AddCalculatedPropertiesAsync(_context);

            // We have a 48 hour back off timer on rating a given path, to try and avoid rating abuse
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
                if (TimeSinceLastRated.TotalHours > 48)
                {
                    RatingAccepted = true;
                }
            }
            // To keep the score somewhat fresh we'll recalculate score on every 10 reads.
            if (Path.Reads % 10 == 0)
            {
                _ = await Path.ApplyPathRatingAsync(_context);
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
            _ = await Path.ApplyPathRatingAsync(_context);
            RatingAcknowledged = true;
            RatingAccepted = false; 
            return Page();
        }
    }
}
