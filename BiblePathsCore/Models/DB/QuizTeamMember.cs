using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB;

public partial class QuizTeamMember
{
    public int Id { get; set; }

    public int? TeamId { get; set; }

    public string Name { get; set; }

    public string Email { get; set; }

    public string Owner { get; set; }

    public int MemberType { get; set; }

    public DateTimeOffset? Created { get; set; }

    public DateTimeOffset? Modified { get; set; }

    public virtual ICollection<QuizTeamMemberAssignment> QuizTeamMemberAssignments { get; set; } = new List<QuizTeamMemberAssignment>();

    public virtual QuizTeam Team { get; set; }

}
