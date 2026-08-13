using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyLeague.Domain.Entities.Auth;

public enum TokenStatus
{
    Active,
    Inactive,
    Expired,
    Banned
}
