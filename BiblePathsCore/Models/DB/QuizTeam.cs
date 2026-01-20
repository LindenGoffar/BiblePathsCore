using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB;

public partial class QuizTeam
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Owner { get; set; }

    public int TeamType { get; set; }

    public DateTimeOffset? Created { get; set; }

    public DateTimeOffset? Modified { get; set; }

    public virtual ICollection<QuizTeamCoach> QuizTeamCoaches { get; set; } = new List<QuizTeamCoach>();

    public virtual ICollection<QuizTeamMemberAssignment> QuizTeamMemberAssignments { get; set; } = new List<QuizTeamMemberAssignment>();

    public virtual ICollection<QuizTeamMember> QuizTeamMembers { get; set; } = new List<QuizTeamMember>();
}
