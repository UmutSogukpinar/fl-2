namespace FantasyLeague.Domain.Entities;

public class NbaPlayer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public int NbaId { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public string? Team { get; set; }

    public string? Position { get; set; }

    public int? JerseyNumber { get; set; }

    public int? HeightCm { get; set; }

    public decimal? WeightKg { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<PlayerStats> SeasonStats { get; set; } = [] ;
}
