using System;
using System.Collections.Generic;

#nullable disable

namespace BiblePathsCore.Models.DB
{
    public partial class GameTeam
    {
        public int Id { get; set; }
        public int? GroupId { get; set; }
        public string Name { get; set; }
        public int CurrentStepId { get; set; }
        public int TeamType { get; set; }
        public int BoardState { get; set; }
        public string KeyWord { get; set; }
        public string GuideWord { get; set; }
        public DateTimeOffset? Created { get; set; }
        public DateTimeOffset? Modified { get; set; }

        public virtual GameGroup Group { get; set; }
    }
}
