using FantasyLeague.Notification.Application.Common.Interfaces;
using FantasyLeague.Notification.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Notification.Infrastructure.Messaging.Inbox;

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
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        await using var context =
            await contextFactory.CreateDbContextAsync(cancellation);
        var messages = context.Set<InboxMessage>();
        var inboxMessage = await messages.SingleOrDefaultAsync(
            message => message.MessageId == messageId,
            cancellation);

        if (inboxMessage?.ProcessedOnUtc is not null)
            return false;

        if (inboxMessage is null)
        {
            inboxMessage = new InboxMessage
            {
                MessageId = messageId,
                MessageType = messageType,
                Payload = payload
            };

            messages.Add(inboxMessage);
        }

        inboxMessage.AttemptCount++;

        await context.SaveChangesAsync(cancellation);
        return true;
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

        return await context.Set<InboxMessage>().SingleOrDefaultAsync(
            message => message.MessageId == messageId,
            cancellation)
            ?? throw new InvalidOperationException(
                $"Inbox message '{messageId}' was not found.");
    }
}
