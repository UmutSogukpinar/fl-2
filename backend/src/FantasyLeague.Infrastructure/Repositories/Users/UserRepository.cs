using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Infrastructure.Context;

namespace FantasyLeague.Infrastructure.Repositories.Users;

public sealed partial class UserRepository(
    AppDbContext dbContext) : IUserRepository;
