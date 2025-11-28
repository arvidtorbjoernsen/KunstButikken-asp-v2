using KunstButikken.AuctionService.Domain.Interfaces;
using KunstButikken.AuctionService.Domain.Models;
using KunstButikken.Common.Logging;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.IntegrationEvents.Contracts.Events;

namespace KunstButikken.AuctionService.IntegrationEvents.Handlers;

public class ArtUpdatedIntegrationEventHandler(
    IAuctionRepository repository,
    ILogger<ArtUpdatedIntegrationEventHandler> logger) : IIntegrationEventConsumer<ArtUpdatedIntegrationEvent>
{
    public async Task Handle(ArtUpdatedIntegrationEvent integrationEvent)
    {
        if (integrationEvent is null)
        {
            throw new ArgumentNullException(nameof(integrationEvent));
        }

        var art = await repository.GetArtByIdAsync(integrationEvent.Id).ConfigureAwait(false);
        if (art is null)
        {
            LogMessages.Warning_ArtId_30(logger, integrationEvent.Id, null);
            return;
        }

        art.Title = string.IsNullOrWhiteSpace(integrationEvent.TitleEn)
            ? integrationEvent.TitleNb
            : integrationEvent.TitleEn;
        art.Artist = integrationEvent.Artist;
        art.Price = integrationEvent.Price;

        await repository.SaveChangesAsync().ConfigureAwait(false);

        if (integrationEvent.IsVerified && integrationEvent.Status == "Published")
        {
            var auction = new Auction
            {
                ArtId = art.Id, StartingPrice = art.Price, EndsAt = DateTime.UtcNow.AddDays(7)
            };
            await repository.AddAsync(auction).ConfigureAwait(false);
            await repository.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
