namespace FantasyLeague.Domain.Entities;

public class FantasyTeam
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public required Guid LeagueId { get; set; }

    public required Guid OwnerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<FantasyTeamPlayer> Players { get; set; } = [];
}
