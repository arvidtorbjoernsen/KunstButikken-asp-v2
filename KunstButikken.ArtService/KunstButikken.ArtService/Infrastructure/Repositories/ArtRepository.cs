using KunstButikken.ArtService.Infrastructure.Data;
using KunstButikken.ArtService.Domain.Interfaces;
using KunstButikken.ArtService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.ArtService.Infrastructure.Repositories;

public class ArtRepository : IArtRepository
{
    private readonly ArtDbContext _db;

    public ArtRepository(ArtDbContext db)
    {
        _db = db;
    }

    public IQueryable<Art> Query() => _db.Arts.AsQueryable();

    public Task<Art?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Arts.FindAsync(new object[] { id }, cancellationToken).AsTask();
    }

    public Task AddAsync(Art art, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(art);

        _db.Arts.Add(art);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Art art, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(art);

        _db.Arts.Remove(art);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public Task<List<Art>> GetBySellerAsync(Guid sellerId, bool includeUnverified = true, CancellationToken cancellationToken = default)
    {
        var query = _db.Arts.Where(a => a.SellerId == sellerId);
        if (!includeUnverified)
        {
            query = query.Where(a => a.IsVerified && a.Status == ArtStatus.Published);
        }
        return query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
