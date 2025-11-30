using KunstButikken.AuctionService.Domain.Models;

namespace KunstButikken.AuctionService.Application.Interfaces;

public interface IAuctionService
{
    Task<IReadOnlyList<Auction>> GetAllAsync(AuctionStatus? status = null, bool includeBids = false, CancellationToken ct = default);
    Task<Auction?> GetByIdAsync(Guid id, bool includeBids = false, CancellationToken ct = default);
    Task<Auction> CreateAsync(Auction auction, CancellationToken ct = default);
    Task<Auction?> UpdateAsync(Guid id, Auction updatedAuction, Guid? currentUserId, CancellationToken ct = default);
    Task<Auction?> PlaceBidAsync(Guid auctionId, decimal amount, Guid bidderId, CancellationToken ct = default);
    Task<Auction?> CloseAsync(Guid auctionId, CancellationToken ct = default);
}
