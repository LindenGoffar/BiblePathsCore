using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.ViewComponents
{
    public class TeamStepsViewComponent : ViewComponent
    {
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;
        public TeamStepsViewComponent(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int GroupId, int TeamId)
        {
            List<PathNode> TeamSteps = new List<PathNode>();
            GameGroup Group = await _context.GameGroups.FindAsync(GroupId);
            GameTeam Team = await _context.GameTeams.FindAsync(TeamId);
            string BibleId = await GameTeam.GetValidBibleIdAsync(_context, null);

            Path Path = await _context.Paths.FindAsync(Group.PathId);
            _ = await Path.AddCalculatedPropertiesAsync(_context);

            if (Team.BoardState == (int)GameTeam.GameBoardState.StepSelect)
            {
                TeamSteps = await Team.GetTeamStepsAsync(_context, BibleId);
            }
            Team.Steps = TeamSteps;
            return View(Team);
        }
    }
}
