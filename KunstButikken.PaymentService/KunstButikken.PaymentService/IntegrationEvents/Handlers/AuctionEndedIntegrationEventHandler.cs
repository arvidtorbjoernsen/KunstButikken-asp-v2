using KunstButikken.Common.Logging;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.IntegrationEvents.Contracts.Events;

// Updated namespace

namespace KunstButikken.PaymentService.IntegrationEvents.Handlers;

public class AuctionEndedIntegrationEventHandler(ILogger<AuctionEndedIntegrationEventHandler> logger)
    : IIntegrationEventConsumer<AuctionEndedIntegrationEvent>
{
    public Task Handle(AuctionEndedIntegrationEvent integrationEvent)
    {
        if (integrationEvent is null)
        {
            throw new ArgumentNullException(nameof(integrationEvent));
        }

        LogMessages.Information_AuctionId_55(logger, integrationEvent.AuctionId, null);
        return Task.CompletedTask;
    }
}
