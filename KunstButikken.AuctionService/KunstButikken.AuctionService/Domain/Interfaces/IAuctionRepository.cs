using KunstButikken.AuctionService.Domain.Models;

namespace KunstButikken.AuctionService.Domain.Interfaces;

public interface IAuctionRepository
{
    IQueryable<Auction> Query();
    Task AddAsync(Auction a, CancellationToken ct = default);
    Task<Auction?> FindAsync(Guid id, CancellationToken ct = default);
    Task AddBidAsync(Bid b, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    // Art management (integration event handlers expect these)
    Task AddArtAsync(Art art, CancellationToken cancellationToken = default);
    Task<Art?> GetArtByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void RemoveArt(Art art);
}
