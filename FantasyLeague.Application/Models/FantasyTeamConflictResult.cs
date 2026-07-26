using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyLeague.Application.Models;

[Flags]
public enum FastasyTeamConflictResult
{
    None = 0,
    OwnerHasMultipleTeam = 1 << 0,
    NameIsTaken = 1 << 1
}