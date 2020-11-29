using System;
using System.Collections.Generic;

#nullable disable

namespace BiblePathsCore.Models.DB
{
    public partial class GameGroup
    {
        public GameGroup()
        {
            GameTeams = new HashSet<GameTeam>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public int? PathId { get; set; }
        public int GroupType { get; set; }
        public int GroupState { get; set; }
        public DateTimeOffset? Created { get; set; }
        public DateTimeOffset? Modified { get; set; }

        public virtual Path Path { get; set; }
        public virtual ICollection<GameTeam> GameTeams { get; set; }
    }
}
