namespace KunstButikken.IntegrationEvents.Contracts.Abstractions;

public interface IIntegrationEventConsumer<in TIntegrationEvent>
  where TIntegrationEvent : IntegrationEvent
{
  Task Handle(TIntegrationEvent integrationEvent);
}