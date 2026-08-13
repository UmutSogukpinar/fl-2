using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Infrastructure.Context;

namespace FantasyLeague.Infrastructure.Repositories.Drafts;

public sealed partial class DraftRepository(
    AppDbContext dbContext) : IDraftRepository;
