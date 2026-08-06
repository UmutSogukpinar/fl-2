using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyLeague.Domain.Entities.Users;

[Flags]
public enum UserRole
{
    User = 1,
    Admin = 1 << 1
}
