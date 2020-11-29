using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.ViewComponents
{
    public class RelatedPathsViewComponent : ViewComponent
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;
        public RelatedPathsViewComponent(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int PathId)
        {
            Path Path = await _context.Paths.FindAsync(PathId);
            List<Path> ReturnPaths = await Path.GetRelatedPathsAsync(_context);
            foreach (Path relatedPath in ReturnPaths)
            {
                _ = await relatedPath.AddCalculatedPropertiesAsync(_context);
            }
            return View(ReturnPaths);
        }
    }
}
