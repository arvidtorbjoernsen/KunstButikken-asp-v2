namespace KunstButikken.IntegrationEvents.Contracts.Abstractions;

public interface IEventBus
{
  Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default) where T : IntegrationEvent;

  void Subscribe<T, TH>()
    where T : IntegrationEvent
    where TH : IIntegrationEventConsumer<T>;

  Task SubscribeAsync<T, TH>()
    where T : IntegrationEvent
    where TH : IIntegrationEventConsumer<T>;
}