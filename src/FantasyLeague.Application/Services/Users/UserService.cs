using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.Common.Interfaces.Security;
using FantasyLeague.Application.Services.Auth;

namespace FantasyLeague.Application.Services.Users;

public sealed partial class UserService(
    IUserRepository _userRepository,
    IPasswordHasher _passwordHasher,
    IJwtService _jwtService) : IUserService;
