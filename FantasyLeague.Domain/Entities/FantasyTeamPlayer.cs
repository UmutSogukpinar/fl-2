using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Domain.Entities;

public class FantasyTeamPlayer
{
    public Guid FantasyTeamId { get; set; }
    public Guid LeagueId { get; set; }
    public Guid NbaPlayerId { get; set; }
    public RosterSlot Slot { get; set; } = RosterSlot.Active;
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
}
