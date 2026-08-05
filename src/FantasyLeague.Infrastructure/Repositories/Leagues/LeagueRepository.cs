using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Infrastructure.Context;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed partial class LeagueRepository(
    AppDbContext dbContext) : ILeagueRepository;
