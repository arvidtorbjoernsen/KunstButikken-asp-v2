using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KunstButikken.AuctionService.Domain.Interfaces;
using KunstButikken.AuctionService.Domain.Models;
using KunstButikken.AuctionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.AuctionService.Infrastructure.Repositories;

public class AuctionRepository : IAuctionRepository
{
    private readonly AuctionDbContext _db;

    public AuctionRepository(AuctionDbContext db)
    {
        _db = db;
    }

    public IQueryable<Auction> Query() => _db.Auctions.AsQueryable();

    public Task AddAsync(Auction a, CancellationToken ct = default) => _db.Auctions.AddAsync(a, ct).AsTask();

    public Task<Auction?> FindAsync(Guid id, CancellationToken ct = default) => _db.Auctions.FindAsync(new object[] { id }, ct).AsTask();

    public Task AddBidAsync(Bid b, CancellationToken ct = default) => _db.Bids.AddAsync(b, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    // Art operations
    public Task AddArtAsync(Art art, CancellationToken cancellationToken = default) => _db.Arts.AddAsync(art, cancellationToken).AsTask();

    public Task<Art?> GetArtByIdAsync(Guid id, CancellationToken cancellationToken = default) => _db.Arts.FindAsync(new object[] { id }, cancellationToken).AsTask();

    public void RemoveArt(Art art) => _db.Arts.Remove(art);

    public Task<List<Auction>> GetBySellerAsync(Guid sellerId, bool includeClosed = true, CancellationToken ct = default)
    {
        var query = _db.Auctions.Where(a => a.SellerId == sellerId);
        if (!includeClosed)
        {
            query = query.Where(a => a.Status != AuctionStatus.Closed);
        }
        return query
            .OrderByDescending(a => a.StartsAt)
            .ToListAsync(ct);
    }
}
