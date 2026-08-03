namespace FantasyLeague.Domain.Entities.Transfers;

public class TransferRequestPlayer
{
    public Guid TransferRequestId { get; set; }
    public Guid FromTeamId { get; set; }
    public Guid NbaPlayerId { get; set; }
}
