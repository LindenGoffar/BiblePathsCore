using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class GameGroup
    {
        public enum GameGroupState { Open, InPlay, SelectPath, Closed}
        public static async Task<List<SelectListItem>> GetPathSelectListAsync(BiblePathsCoreDbContext context)
        {
            List<SelectListItem> PathSelectList = new List<SelectListItem>();

            List<Path> Paths = await context.Paths
                                                .Where(P => P.IsDeleted == false
                                                        && P.IsPublished == true)
                                                .OrderBy(P => P.StepCount)
                                                .ToListAsync();
            // Add a Default entry 
            PathSelectList.Add(new SelectListItem
            {
                Text = "Select a Bible Path",
                Value = 0.ToString()
            });

            // Add our BookLists first
            foreach (Path path in Paths)
            {
                PathSelectList.Add(new SelectListItem
                {
                    Text = path.Name + " - " + path.StepCount + " Steps",
                    Value = path.Id.ToString()
                });
            }
            return PathSelectList;
        }
    }

    public partial class GameTeam
    {
        public enum GameBoardState { Initialize, WordSelect, StepSelect, Completed, Closed }
    }
}
