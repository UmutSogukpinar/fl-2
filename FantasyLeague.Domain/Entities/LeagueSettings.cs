namespace FantasyLeague.Domain.Entities;

public class LeagueSettings
{
    public Guid LeagueId { get; set; }

    public int RosterSize { get; set; } = 13;

    public DateTime? DraftDate { get; set; }

    public string DraftTimeZoneId { get; set; } = "UTC";

    public DateTime? UpdatedAt { get; set; }
}
