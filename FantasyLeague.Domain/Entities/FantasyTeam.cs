namespace FantasyLeague.Domain.Entities;

public class FantasyTeam
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public Guid LeagueId { get; set; }

    public Guid OwnerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public required User Owner { get; set; }
}
