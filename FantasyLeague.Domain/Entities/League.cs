namespace FantasyLeague.Domain.Entities;

public class League
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int Season { get; set; }

    public int MaxTeams { get; set; } = 10;

    public Guid CommissionerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public required User Commissioner { get; set; }
}
