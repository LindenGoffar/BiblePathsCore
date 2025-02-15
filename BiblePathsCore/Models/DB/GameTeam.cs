﻿using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB;

public partial class GameTeam
{
    public int Id { get; set; }

    public int? GroupId { get; set; }

    public string Name { get; set; }

    public int CurrentStepId { get; set; }

    public int StepNumber { get; set; }

    public int TeamType { get; set; }

    public int BoardState { get; set; }

    public string KeyWord { get; set; }

    public string GuideWord { get; set; }

    public DateTimeOffset? Created { get; set; }

    public DateTimeOffset? Modified { get; set; }

    public DateTimeOffset? GameStarted { get; set; }

    public DateTimeOffset? GameCompleted { get; set; }

    public virtual GameGroup Group { get; set; }
}
