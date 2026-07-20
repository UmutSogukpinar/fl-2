namespace FantasyLeague.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Username { get; set; }

    public required string Email { get; set; }

    public required string Password { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<League> CommissionedLeagues { get; set; } = new List<League>();

    public ICollection<FantasyTeam> FantasyTeams { get; set; } = new List<FantasyTeam>();
}
