using KunstButikken.ArtService.Application.Interfaces;
using KunstButikken.ArtService.Domain.Interfaces;
using KunstButikken.ArtService.Domain.Models;
using KunstButikken.ArtService.Domain.DTOs;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.IntegrationEvents.Contracts.Events;
using Microsoft.Extensions.Logging;
using KunstButikken.ServiceDefaults;

namespace KunstButikken.ArtService.Application.Services;

public class ArtService : IArtService
{
    private readonly IArtRepository _repo;
    private readonly IBlobStorage _blob;
    private readonly IEventBus _eventBus;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<ArtService> _logger;

    public ArtService(IArtRepository repo, IBlobStorage blob, IEventBus eventBus, IDateTimeProvider clock, ILogger<ArtService> logger)
    {
        _repo = repo;
        _blob = blob;
        _eventBus = eventBus;
        _clock = clock;
        _logger = logger;
    }

    public Task<IEnumerable<Art>> GetAllAsync(ArtStatus? status = null, bool? featured = null, CancellationToken ct = default)
    {
        var query = _repo.Query();

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (featured.HasValue)
        {
            query = query.Where(a => a.IsFeatured == featured.Value);
        }

        if (!status.HasValue)
        {
            query = query.Where(a => a.Status == ArtStatus.Published);
        }

        if (!status.HasValue && !featured.HasValue)
        {
            query = query.Where(a => a.IsVerified);
        }

        return Task.FromResult<IEnumerable<Art>>(query.OrderByDescending(a => a.CreatedAt).ToList());
    }

    public Task<Art?> GetByIdAsync(Guid id, CancellationToken ct = default) => _repo.FindAsync(id, ct);

    public async Task<Art> CreateAsync(Art art, CancellationToken ct = default)
    {
        art.Id = Guid.NewGuid();
        art.Status = ArtStatus.Draft;
        art.CreatedAt = _clock.UtcNow;
        await _repo.AddAsync(art, ct).ConfigureAwait(false);
        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
        var ev = new KunstButikken.IntegrationEvents.Contracts.Events.ArtCreatedIntegrationEvent(
            art.TitleEn, art.TitleNb, art.Artist, art.SellerId, art.SellerDisplayName, art.Price, art.ImageUrl);
        await _eventBus.PublishAsync(ev, ct).ConfigureAwait(false);
        return art;
    }

    public async Task UpdateAsync(Guid id, Art update, CancellationToken ct = default)
    {
        var art = await _repo.FindAsync(id, ct).ConfigureAwait(false);
        if (art == null) throw new KeyNotFoundException();
        art.TitleNb = update.TitleNb;
        art.TitleEn = update.TitleEn;
        art.DescriptionNb = update.DescriptionNb;
        art.DescriptionEn = update.DescriptionEn;
        art.ImageUrl = update.ImageUrl;
        art.Price = update.Price;
        art.Artist = update.Artist;
        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
        var ev = new KunstButikken.IntegrationEvents.Contracts.Events.ArtUpdatedIntegrationEvent(
            art.Id, art.TitleEn, art.TitleNb, art.Artist, art.SellerId, art.SellerDisplayName, art.Price, art.ImageUrl, art.Status.ToString(), art.IsVerified);
        await _eventBus.PublishAsync(ev, ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var art = await _repo.FindAsync(id, ct).ConfigureAwait(false);
        if (art == null) return;
        await _repo.RemoveAsync(art, ct).ConfigureAwait(false);
        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
        var ev = new KunstButikken.IntegrationEvents.Contracts.Events.ArtDeletedIntegrationEvent(art.Id);
        await _eventBus.PublishAsync(ev, ct).ConfigureAwait(false);
    }

    public async Task<ImageUploadResponse> UploadImageAsync(Guid id, Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var art = await _repo.FindAsync(id, ct).ConfigureAwait(false);
        if (art == null)
        {
            throw new KeyNotFoundException();
        }

        var ext = Path.GetExtension(fileName);
        var blobName = $"{id}/{Guid.NewGuid()}{ext}";
        var url = await _blob.UploadAsync(blobName, fileStream, contentType, ct).ConfigureAwait(false);
        art.ImageUrl = new Uri(url);
        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
        return new ImageUploadResponse { ImageUrl = art.ImageUrl };
    }

    public Task<IEnumerable<Art>> GetFeaturedAsync(int limit, CancellationToken ct = default)
    {
        var query = _repo.Query().Where(a => a.IsFeatured && a.IsVerified && a.Status == ArtStatus.Published)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .AsQueryable();
        return Task.FromResult<IEnumerable<Art>>(query.ToList());
    }

    public Task<IEnumerable<Art>> GetUnverifiedAsync(CancellationToken ct = default)
    {
        var query = _repo.Query().Where(a => !a.IsVerified).OrderByDescending(a => a.CreatedAt);
        return Task.FromResult<IEnumerable<Art>>(query.ToList());
    }

    public Task<IEnumerable<Art>> GetBySellerAsync(Guid sellerId, bool includeUnverified = true, CancellationToken ct = default)
    {
        return _repo.GetBySellerAsync(sellerId, includeUnverified, ct).ContinueWith(t => (IEnumerable<Art>)t.Result, ct,
            TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.Default);
    }
}
