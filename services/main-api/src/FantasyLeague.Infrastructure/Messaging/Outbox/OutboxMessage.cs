namespace FantasyLeague.Infrastructure.Messaging.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string PublisherName { get; set; }
    public required string MessageType { get; set; }
    public required string Payload { get; set; }
    public DateTime OccurredOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedOnUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
