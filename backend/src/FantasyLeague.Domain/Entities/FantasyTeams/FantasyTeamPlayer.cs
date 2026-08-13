namespace FantasyLeague.Domain.Entities.FantasyTeams;

public class FantasyTeamPlayer
{
    public Guid FantasyTeamId { get; set; }
    public Guid LeagueId { get; set; }
    public Guid NbaPlayerId { get; set; }
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
}
