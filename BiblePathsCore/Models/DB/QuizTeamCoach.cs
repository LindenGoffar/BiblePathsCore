using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB;

public partial class QuizTeamCoach
{
    public int Id { get; set; }

    public int? TeamId { get; set; }

    public int? CoachId { get; set; }

    public int CoachType { get; set; }

    public DateTimeOffset? Created { get; set; }

    public DateTimeOffset? Modified { get; set; }

    public virtual QuizUser Coach { get; set; }

    public virtual QuizTeam Team { get; set; }
}
