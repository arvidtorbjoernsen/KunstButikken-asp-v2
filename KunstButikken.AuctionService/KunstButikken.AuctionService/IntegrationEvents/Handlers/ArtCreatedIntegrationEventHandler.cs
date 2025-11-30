using KunstButikken.AuctionService.Domain.Interfaces;
using KunstButikken.AuctionService.Domain.Models;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.IntegrationEvents.Contracts.Events;

// Updated namespace

namespace KunstButikken.AuctionService.IntegrationEvents.Handlers;

public class ArtCreatedIntegrationEventHandler(IAuctionRepository repository)
    : IIntegrationEventConsumer<ArtCreatedIntegrationEvent>
{
    public async Task Handle(ArtCreatedIntegrationEvent integrationEvent)
    {
        if (integrationEvent is null)
        {
            throw new ArgumentNullException(nameof(integrationEvent));
        }

        var art = new Art
        {
            Id = integrationEvent.Id,
            Title = string.IsNullOrWhiteSpace(integrationEvent.TitleEn) ? integrationEvent.TitleNb : integrationEvent.TitleEn,
            Artist = integrationEvent.Artist,
            Price = integrationEvent.Price,
            SellerId = integrationEvent.SellerId.ToString()
        };

        await repository.AddArtAsync(art).ConfigureAwait(false);
        await repository.SaveChangesAsync().ConfigureAwait(false);
    }
}
