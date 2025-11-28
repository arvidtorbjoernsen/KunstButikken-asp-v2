using KunstButikken.IntegrationEvents.Contracts;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;

namespace KunstButikken.ArtService.Tests.Fakes;

internal sealed class FakeEventBus : IEventBus
{
    public IntegrationEvent? PublishedEvent { get; private set; }

    public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default) where T : IntegrationEvent
    {
        PublishedEvent = integrationEvent;
        return Task.CompletedTask;
    }

    public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventConsumer<T>
    {
        // no-op for tests
    }

    public Task SubscribeAsync<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventConsumer<T>
    {
        return Task.CompletedTask;
    }
}

