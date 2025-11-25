using System.Linq;
using KunstButikken.AuctionService.Domain.Models;

namespace KunstButikken.AuctionService.Application.Interfaces;

public interface IAuctionService
{
    IQueryable<Auction> Query();
    Task<IEnumerable<Auction>> GetAllAsync(CancellationToken ct = default);
    Task<Auction?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Auction> CreateAsync(Auction auction, CancellationToken ct = default);
    Task AddAsync(Auction auction, CancellationToken ct = default);
    Task AddBidAsync(Bid bid, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task BidAsync(Guid auctionId, Bid bid, CancellationToken ct = default);
}
