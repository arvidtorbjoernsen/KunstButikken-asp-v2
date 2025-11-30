using KunstButikken.AuctionService.Domain.Interfaces;
using KunstButikken.AuctionService.Domain.Models;

namespace KunstButikken.AuctionService.Tests.TestDoubles;

internal sealed class FakeAuctionRepository : IAuctionRepository
{
    private readonly List<Auction> _auctions = new();
    private readonly List<Art> _arts = new();

    public int SaveCallCount { get; private set; }

    public IReadOnlyList<Auction> Auctions => _auctions;
    public IReadOnlyList<Art> Arts => _arts;

    public IQueryable<Auction> Query() => _auctions.AsQueryable();

    public Task AddAsync(Auction a, CancellationToken ct = default)
    {
        _auctions.Add(a);
        return Task.CompletedTask;
    }

    public Task<Auction?> FindAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_auctions.SingleOrDefault(a => a.Id == id));

    public Task AddBidAsync(Bid b, CancellationToken ct = default)
    {
        var auction = _auctions.FirstOrDefault(a => a.Id == b.AuctionId);
        auction?.Bids.Add(b);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCallCount++;
        return Task.CompletedTask;
    }

    public Task AddArtAsync(Art art, CancellationToken ct = default)
    {
        _arts.Add(art);
        return Task.CompletedTask;
    }

    public Task<Art?> GetArtByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_arts.SingleOrDefault(a => a.Id == id));

    public void RemoveArt(Art art) => _arts.Remove(art);

    public void SeedArt(Art art) => _arts.Add(art);
}

