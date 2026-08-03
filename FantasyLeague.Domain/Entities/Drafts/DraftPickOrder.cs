namespace FantasyLeague.Domain.Entities.Drafts;

public class DraftPickOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LeagueId { get; set; }
    public Guid TeamId { get; set; }
    public int Round { get; set; }
    public int PositionInRound { get; set; }
    public int OverallPick { get; set; }
    public Guid? NbaPlayerId { get; set; }
    public DateTime? PickedAt { get; set; }
}
