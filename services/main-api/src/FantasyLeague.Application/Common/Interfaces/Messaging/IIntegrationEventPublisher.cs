namespace FantasyLeague.Application.Common.Interfaces.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TMessage>(
        string publisherName,
        TMessage message,
        CancellationToken cancellationToken = default);
}
