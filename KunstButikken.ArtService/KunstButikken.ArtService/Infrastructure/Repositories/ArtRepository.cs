using KunstButikken.ArtService.Infrastructure.Data;
using KunstButikken.ArtService.Domain.Interfaces;
using KunstButikken.ArtService.Domain.Models;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.IntegrationEvents.Contracts.Events;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.ArtService.Infrastructure.Repositories;

public class ArtRepository : IArtRepository
{
    private readonly ArtDbContext _db;
    private readonly IEventBus _eventBus;

    public ArtRepository(ArtDbContext db, IEventBus eventBus)
    {
        _db = db;
        _eventBus = eventBus;
    }

    public IQueryable<Art> Query() => _db.Arts.AsQueryable();

    public Task<Art?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Arts.FindAsync(new object[] { id }, cancellationToken).AsTask();
    }

    public async Task AddAsync(Art art, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(art);

        _db.Arts.Add(art);
        var integrationEvent = new ArtCreatedIntegrationEvent(
            art.TitleEn,
            art.TitleNb,
            art.Artist,
            art.SellerId,
            art.SellerDisplayName,
            art.Price,
            art.ImageUrl
        );
        await _eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(Art art, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(art);

        _db.Arts.Remove(art);
        var integrationEvent = new ArtDeletedIntegrationEvent(art.Id);
        await _eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
