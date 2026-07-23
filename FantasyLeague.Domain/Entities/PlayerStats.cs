namespace FantasyLeague.Domain.Entities;

public class PlayerStats
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid NbaPlayerId { get; set; }

    public int Season { get; set; } = 2025;

    public int GamesPlayed { get; set; }

    public int GamesStarted { get; set; }

    public double MinutesPerGame { get; set; }

    public double PointsPerGame { get; set; }

    public double ReboundsPerGame { get; set; }

    public double AssistsPerGame { get; set; }

    public double StealsPerGame { get; set; }

    public double BlocksPerGame { get; set; }

    public double TurnoversPerGame { get; set; }

    public double FieldGoalPercentage { get; set; }

    public double ThreePointPercentage { get; set; }

    public double FreeThrowPercentage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // TODO: Consider whether is necessary or not
    // Does this cause a circular reference issue when serializing to JSON?
    // and increase tight coupling?
    // If I need to remove it, Should I create another migration?
    public NbaPlayer? NbaPlayer { get; set; }
}
