using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB;

public partial class QuizTeamMemberAssignment
{
    public int Id { get; set; }

    public int? TeamId { get; set; }

    public int? MemberId { get; set; }

    public int? BookNumber { get; set; }

    public int? ChapterNumber { get; set; }

    public int AssignmentType { get; set; }

    public DateTimeOffset? Created { get; set; }

    public DateTimeOffset? Modified { get; set; }

    public virtual QuizTeamMember Member { get; set; }

    public virtual QuizTeam Team { get; set; }
}
