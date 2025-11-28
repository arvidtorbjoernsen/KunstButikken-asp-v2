using KunstButikken.AuctionService.Application.Interfaces;
using KunstButikken.AuctionService.Domain.Models;
using KunstButikken.AuctionService.Interfaces;
using Microsoft.EntityFrameworkCore;
using KunstButikken.ServiceDefaults;

namespace KunstButikken.AuctionService.Application.Services;

public sealed class AuctionAppService : IAuctionService
{
    private readonly IAuctionRepository _repo;
    private readonly IDateTimeProvider _clock;

    public AuctionAppService(IAuctionRepository repo, IDateTimeProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public async Task<IReadOnlyList<Auction>> GetAllAsync(AuctionStatus? status = null, bool includeBids = false, CancellationToken ct = default)
    {
        var query = includeBids ? _repo.Query().Include(a => a.Bids) : _repo.Query();
        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }
        return await query.OrderByDescending(a => a.StartsAt).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<Auction?> GetByIdAsync(Guid id, bool includeBids = false, CancellationToken ct = default)
    {
        var query = includeBids ? _repo.Query().Include(a => a.Bids) : _repo.Query();
        return await query.FirstOrDefaultAsync(a => a.Id == id, ct).ConfigureAwait(false);
    }

    public async Task<Auction> CreateAsync(Auction auction, CancellationToken ct = default)
    {
        auction.Id = Guid.NewGuid();
        auction.Status = AuctionStatus.Open;
        await _repo.AddAsync(auction, ct).ConfigureAwait(false);
        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
        return auction;
    }

    public async Task<Auction?> UpdateAsync(Guid id, Auction updatedAuction, Guid? currentUserId, CancellationToken ct = default)
    {
        var auction = await _repo.Query().Include(a => a.Bids).FirstOrDefaultAsync(a => a.Id == id, ct).ConfigureAwait(false);
        if (auction is null)
        {
            return null;
        }

        if (currentUserId.HasValue && auction.SellerId != currentUserId.Value)
        {
            throw new UnauthorizedAccessException("Seller mismatch");
        }

        if (auction.Bids.Count > 0)
        {
            throw new InvalidOperationException("Cannot edit an auction with bids");
        }

        auction.StartsAt = updatedAuction.StartsAt;
        auction.EndsAt = updatedAuction.EndsAt;
        auction.StartingPrice = updatedAuction.StartingPrice;
        auction.ReservePrice = updatedAuction.ReservePrice;

        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
        return auction;
    }

    public async Task<Auction?> PlaceBidAsync(Guid auctionId, decimal amount, Guid bidderId, CancellationToken ct = default)
    {
        var auction = await _repo.Query().Include(a => a.Bids).FirstOrDefaultAsync(a => a.Id == auctionId, ct).ConfigureAwait(false);
        if (auction is null)
        {
            return null;
        }

        if (auction.Status != AuctionStatus.Open || auction.EndsAt <= _clock.UtcNow)
        {
            throw new InvalidOperationException("Auction closed");
        }

        var min = auction.Bids.Count == 0 ? auction.StartingPrice : auction.Bids.Max(b => b.Amount);
        if (amount <= min)
        {
            throw new InvalidOperationException($"Bid must be greater than {min}");
        }

        var bid = new Bid
        {
            Id = Guid.NewGuid(),
            AuctionId = auctionId,
            BidderId = bidderId,
            Amount = amount
        };
        await _repo.AddBidAsync(bid, ct).ConfigureAwait(false);
        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
        return auction;
    }

    public async Task<Auction?> CloseAsync(Guid auctionId, CancellationToken ct = default)
    {
        var auction = await _repo.Query().Include(a => a.Bids).FirstOrDefaultAsync(a => a.Id == auctionId, ct).ConfigureAwait(false);
        if (auction is null)
        {
            return null;
        }

        auction.Status = AuctionStatus.Closed;
        var top = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
        var reserveOk = !auction.ReservePrice.HasValue || top != null && top.Amount >= auction.ReservePrice.Value;
        if (top != null && reserveOk)
        {
            auction.WinningBid = top.Amount;
            auction.WinnerId = top.BidderId;
        }
        else
        {
            auction.WinningBid = null;
            auction.WinnerId = null;
        }

        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
        return auction;
    }
}
