using KunstButikken.AuctionService.Domain.Interfaces;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.IntegrationEvents.Contracts.Events;

namespace KunstButikken.AuctionService.IntegrationEvents.Handlers;

public class ArtDeletedIntegrationEventHandler(IAuctionRepository repository)
    : IIntegrationEventConsumer<ArtDeletedIntegrationEvent>
{
    public async Task Handle(ArtDeletedIntegrationEvent integrationEvent)
    {
        if (integrationEvent is null)
        {
            throw new ArgumentNullException(nameof(integrationEvent));
        }

        var art = await repository.GetArtByIdAsync(integrationEvent.ArtId).ConfigureAwait(false);
        if (art is not null)
        {
            repository.RemoveArt(art);
            await repository.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
