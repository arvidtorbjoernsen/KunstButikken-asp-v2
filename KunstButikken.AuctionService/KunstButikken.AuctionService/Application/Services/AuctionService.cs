using KunstButikken.AuctionService.Application.Interfaces;
using KunstButikken.AuctionService.Domain.Models;
using KunstButikken.AuctionService.Interfaces;

namespace KunstButikken.AuctionService.Application.Services;

public class AuctionAppService : IAuctionService
{
    private readonly IAuctionRepository _repo;

    public AuctionAppService(IAuctionRepository repo)
    {
        _repo = repo;
    }

    public IQueryable<Auction> Query() => _repo.Query();

    public Task<IEnumerable<Auction>> GetAllAsync(CancellationToken ct = default)
    {
        var q = _repo.Query().AsEnumerable();
        return Task.FromResult(q);
    }

    public Task<Auction?> GetAsync(Guid id, CancellationToken ct = default) => _repo.FindAsync(id, ct);

    public async Task<Auction> CreateAsync(Auction auction, CancellationToken ct = default)
    {
        await _repo.AddAsync(auction, ct).ConfigureAwait(false);
        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
        return auction;
    }

    public Task AddAsync(Auction auction, CancellationToken ct = default) => _repo.AddAsync(auction, ct);

    public Task AddBidAsync(Bid bid, CancellationToken ct = default) => _repo.AddBidAsync(bid, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _repo.SaveChangesAsync(ct);

    public async Task BidAsync(Guid auctionId, Bid bid, CancellationToken ct = default)
    {
        await _repo.AddBidAsync(bid, ct).ConfigureAwait(false);
        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
