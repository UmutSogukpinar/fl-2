namespace FantasyLeague.Domain.Entities;

public class LeagueFixture
{
    public long Id { get; set; }
    public Guid LeagueId { get; set; }
    public int Week { get; set; }
    public Guid HomeTeamId { get; set; }
    public Guid AwayTeamId { get; set; }

    public int? HomeScore {get; set;}
    public int? AwayScore {get; set;}

    public DateTime? GameTime {get; set;}
}
