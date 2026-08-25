namespace FantasyLeague.Notification.Application.Common.Interfaces;

public interface IInboxMessageStore
{
    Task<bool> TryStartProcessingAsync(
        string messageId,
        string messageType,
        string payload,
        CancellationToken cancellation = default);

    Task MarkProcessedAsync(
        string messageId,
        CancellationToken cancellation = default);

    Task MarkFailedAsync(
        string messageId,
        string error,
        CancellationToken cancellation = default);
}
