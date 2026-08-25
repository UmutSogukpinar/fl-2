namespace FantasyLeague.Notification.Infrastructure.Persistence.Inbox;

public sealed class InboxMessage
{
    public required string MessageId { get; set; }
    public required string MessageType { get; set; }
    public required string Payload { get; set; }
    public DateTime ReceivedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedOnUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
