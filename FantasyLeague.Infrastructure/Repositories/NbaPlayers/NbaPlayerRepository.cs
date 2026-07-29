using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Infrastructure.Context;

namespace FantasyLeague.Infrastructure.Repositories.NbaPlayers;

public sealed partial class NbaPlayerRepository(AppDbContext _dbContext)
    : INbaPlayerRepository;
