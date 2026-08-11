using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Domain.Entities.Transfers;

public class TransferRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InitiatingTeamId { get; set; }
    public Guid CounterpartyTeamId { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public ICollection<TransferRequestPlayer> Players { get; set; } = [];
}
