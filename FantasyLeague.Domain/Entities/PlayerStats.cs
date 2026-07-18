namespace FantasyLeague.Domain.Entities;

public class PlayerStats
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid NbaPlayerId { get; set; }

    public int Season { get; set; } = 2025;

    public int GamesPlayed { get; set; }

    public int GamesStarted { get; set; }

    public decimal MinutesPerGame { get; set; }

    public decimal PointsPerGame { get; set; }

    public decimal ReboundsPerGame { get; set; }

    public decimal AssistsPerGame { get; set; }

    public decimal StealsPerGame { get; set; }

    public decimal BlocksPerGame { get; set; }

    public decimal TurnoversPerGame { get; set; }

    public decimal FieldGoalPercentage { get; set; }

    public decimal ThreePointPercentage { get; set; }

    public decimal FreeThrowPercentage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public required NbaPlayer NbaPlayer { get; set; }
}
