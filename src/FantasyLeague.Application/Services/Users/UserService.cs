using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.Common.Interfaces.Security;

namespace FantasyLeague.Application.Services.Users;

public sealed partial class UserService(
    IUserRepository _userRepository,
    IPasswordHasher _passwordHasher) : IUserService;
