using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class QuizTeam
    {

        public static async Task<List<QuizTeam>> GetAllMyTeamsAsync(BiblePathsCoreDbContext context, QuizUser user)
        {
            // We need to get all Teams where our user is either the Owner, and/or a Coach so let's start by 
            // getting all of the teams the user coaches. 
            List<QuizTeam> coachedTeams = await context.QuizTeamCoaches
                .Where(c => c.CoachId == user.Id)
                .Select(c => c.Team)
                .Distinct()
                .Include(t => t.QuizTeamMembers)
                .Include(t => t.QuizTeamCoaches)
                    .ThenInclude(tc => tc.Coach)
                .Include(t => t.QuizTeamMemberAssignments)
                    .ThenInclude(a => a.Member)
                .ToListAsync();

            // Now let's see if this user owns any additional teams. 
            List<QuizTeam> ownedTeams = await context.QuizTeams
                .Where(t => t.Owner == user.Email)
                .Include(t => t.QuizTeamMembers)
                .Include(t => t.QuizTeamCoaches)
                    .ThenInclude(c => c.Coach)
                .Include(t => t.QuizTeamMemberAssignments)
                    .ThenInclude(a => a.Member)
                .ToListAsync();

            return coachedTeams
                    .Concat(ownedTeams)
                    .DistinctBy(t => t.Id)
                    .ToList();

        }
        public static async Task<List<SelectListItem>> GetMyTeamsSelectListAsync(BiblePathsCoreDbContext context, QuizUser user)
        {
            List<SelectListItem> TeamSelectList = new List<SelectListItem>();
            // We need to get all Teams where our user is either the Owner, and/or a Coach so let's start by 
            // getting all of the teams the user coaches. 
            List<QuizTeam> coachedTeams = await context.QuizTeamCoaches
                .Where(c => c.CoachId == user.Id)
                .Select(c => c.Team)
                .Distinct()
                .Include(t => t.QuizTeamMembers)
                .Include(t => t.QuizTeamCoaches)
                    .ThenInclude(tc => tc.Coach)
                .Include(t => t.QuizTeamMemberAssignments)
                    .ThenInclude(a => a.Member)
                .ToListAsync();

            // Now let's see if this user owns any additional teams. 
            List<QuizTeam> ownedTeams = await context.QuizTeams
                .Where(t => t.Owner == user.Email)
                .Include(t => t.QuizTeamMembers)
                .Include(t => t.QuizTeamCoaches)
                    .ThenInclude(c => c.Coach)
                .Include(t => t.QuizTeamMemberAssignments)
                    .ThenInclude(a => a.Member)
                .ToListAsync();

            List<QuizTeam> CombinedTeams = coachedTeams
                                            .Concat(ownedTeams)
                                            .DistinctBy(t => t.Id)
                                            .ToList();

            // Add a Default entry 
            TeamSelectList.Add(new SelectListItem
            {
                Text = "<Select a Team>",
                Value = 0.ToString()
            });
            foreach (QuizTeam Team in CombinedTeams)
            {
                TeamSelectList.Add(new SelectListItem
                {
                    Text = Team.Name,
                    Value = Team.Id.ToString()
                });
            }

            return TeamSelectList;
        }

        public async Task<bool> DeleteTeamAsync(BiblePathsCoreDbContext context)
        {
            // Remove each of our MemberAssignments
            foreach (var assignment in this.QuizTeamMemberAssignments)
            {
                context.QuizTeamMemberAssignments.Remove(assignment);
            }
            await context.SaveChangesAsync();
            foreach (var coach in this.QuizTeamCoaches)
            {
                context.QuizTeamCoaches.Remove(coach);
            }
            await context.SaveChangesAsync();
            foreach (var member in this.QuizTeamMembers)
            {
                context.QuizTeamMembers.Remove(member);
            }
            await context.SaveChangesAsync();

            context.QuizTeams.Remove(this);
            await context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> DeleteTeamMemberAsync(BiblePathsCoreDbContext context, QuizTeamMember Member)
        {
            // make sure to load our assignments
            await context.Entry(Member)
                        .Collection(m => m.QuizTeamMemberAssignments)
                        .LoadAsync();
            foreach (var assignment in Member.QuizTeamMemberAssignments)
            {
                context.QuizTeamMemberAssignments.Remove(assignment);
            }
            await context.SaveChangesAsync();

            context.QuizTeamMembers.Remove(Member);
            await context.SaveChangesAsync();

            return true;
        }
    }
}
