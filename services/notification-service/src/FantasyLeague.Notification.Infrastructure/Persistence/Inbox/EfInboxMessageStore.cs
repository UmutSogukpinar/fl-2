using FantasyLeague.Notification.Application.Common.Interfaces;
using FantasyLeague.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Notification.Infrastructure.Persistence.Inbox;

public sealed class EfInboxMessageStore(
    IDbContextFactory<NotificationDbContext> contextFactory)
    : IInboxMessageStore
{
    public async Task<bool> TryStartProcessingAsync(
        string messageId,
        string messageType,
        string payload,
        CancellationToken cancellation = default)
    {
        ValidateMessage(messageId, messageType, payload);

        await using var context =
            await contextFactory.CreateDbContextAsync(cancellation);
        var messages = context.Set<InboxMessage>();
        var inboxMessage = await FindMessageAsync(
            context,
            messageId,
            cancellation);

        if (IsAlreadyProcessed(inboxMessage))
            return false;

        inboxMessage ??= AddMessage(
            messages,
            messageId,
            messageType,
            payload);

        inboxMessage.AttemptCount++;

        await context.SaveChangesAsync(cancellation);
        return true;
    }

    private static void ValidateMessage(
        string messageId,
        string messageType,
        string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
    }

    private static Task<InboxMessage?> FindMessageAsync(
        NotificationDbContext context,
        string messageId,
        CancellationToken cancellation)
    {
        return context.Set<InboxMessage>().SingleOrDefaultAsync(
            message => message.MessageId == messageId,
            cancellation);
    }

    private static bool IsAlreadyProcessed(InboxMessage? message)
    {
        return message?.ProcessedOnUtc is not null;
    }

    private static InboxMessage AddMessage(
        DbSet<InboxMessage> messages,
        string messageId,
        string messageType,
        string payload)
    {
        var message = new InboxMessage
        {
            MessageId = messageId,
            MessageType = messageType,
            Payload = payload
        };

        messages.Add(message);
        return message;
    }

    public async Task MarkProcessedAsync(
        string messageId,
        CancellationToken cancellation = default)
    {
        await using var context =
            await contextFactory.CreateDbContextAsync(cancellation);
        var inboxMessage = await GetRequiredMessageAsync(
            context,
            messageId,
            cancellation);

        inboxMessage.ProcessedOnUtc = DateTime.UtcNow;
        inboxMessage.LastError = null;

        await context.SaveChangesAsync(cancellation);
    }

    public async Task MarkFailedAsync(
        string messageId,
        string error,
        CancellationToken cancellation = default)
    {
        await using var context =
            await contextFactory.CreateDbContextAsync(cancellation);
        var inboxMessage = await GetRequiredMessageAsync(
            context,
            messageId,
            cancellation);

        inboxMessage.LastError = error;

        await context.SaveChangesAsync(cancellation);
    }

    private static async Task<InboxMessage> GetRequiredMessageAsync(
        NotificationDbContext context,
        string messageId,
        CancellationToken cancellation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        return await FindMessageAsync(context, messageId, cancellation)
            ?? throw new InvalidOperationException(
                $"Inbox message '{messageId}' was not found.");
    }
}
