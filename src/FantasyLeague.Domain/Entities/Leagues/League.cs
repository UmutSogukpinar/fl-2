namespace FantasyLeague.Domain.Entities.Leagues;

using FantasyLeague.Domain.Enums;

public class League
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int Season { get; set; }

    public int MaxTeams { get; set; } = 10;

    public Guid CommissionerId { get; set; }

    public LeagueStatus Status { get; set; } = LeagueStatus.Created;

    public LeagueSettings Settings { get; set; } = new();

    public string JoinCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
