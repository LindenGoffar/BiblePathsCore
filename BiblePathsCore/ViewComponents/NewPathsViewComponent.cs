using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.ViewComponents
{
    public class NewPathsViewComponent : ViewComponent
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;
        public NewPathsViewComponent(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int TopN)
        {
            int SupersetSize = TopN + 5; // We'll grab 5 more than requested so we can randomize slightly. 

            List<Paths> Superset = await _context.Paths.Where(P => P.IsPublished == true && P.IsDeleted == false)
                                                       .OrderByDescending(P => P.Created).Take(SupersetSize).ToListAsync();
            List<Paths> ReturnPaths = new List<Paths>();

            var random = new Random();
            int PathCount = Superset.Count;
            int MaxReturnPaths = PathCount > TopN ? TopN : PathCount;

            for (int i = 0; i < MaxReturnPaths; i++)
            {
                Paths RandomPath = Superset[random.Next(PathCount)];
                if (!ReturnPaths.Contains(RandomPath))
                {
                    ReturnPaths.Add(RandomPath);
                }
            }
            
            foreach (Paths path in ReturnPaths)
            {
                _ = await path.AddCalculatedPropertiesAsync(_context);
            }
            return View(ReturnPaths);
        }
    }
}
