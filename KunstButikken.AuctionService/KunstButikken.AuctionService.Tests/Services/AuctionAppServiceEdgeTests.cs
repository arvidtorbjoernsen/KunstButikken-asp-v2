using System.Collections.Generic;
using System.Linq;
using KunstButikken.AuctionService.Application.Services;
using KunstButikken.AuctionService.Domain.Interfaces;
using KunstButikken.AuctionService.Domain.Models;
using KunstButikken.AuctionService.Tests.TestDoubles;
using KunstButikken.ServiceDefaults;
using Moq;
using Xunit;

namespace KunstButikken.AuctionService.Tests.Services;

public class AuctionAppServiceEdgeTests
{
    private readonly Mock<IAuctionRepository> _repo = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly AuctionAppService _sut;

    public AuctionAppServiceEdgeTests()
    {
        _sut = new AuctionAppService(_repo.Object, _clock.Object);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenSellerMismatch()
    {
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            StartsAt = DateTimeOffset.UtcNow,
            EndsAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _sut.UpdateAsync(auction.Id, new Auction { StartsAt = auction.StartsAt, EndsAt = auction.EndsAt }, Guid.NewGuid()));
    }

    [Fact]
    public async Task PlaceBidAsync_ThrowsWhenBidNotGreaterThanHighest()
    {
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Status = AuctionStatus.Open,
            StartsAt = DateTimeOffset.UtcNow.AddHours(-1),
            EndsAt = DateTimeOffset.UtcNow.AddHours(1),
            StartingPrice = 100M
        };
        auction.Bids.Add(new Bid { Amount = 150M });
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));
        _clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.PlaceBidAsync(auction.Id, 150M, Guid.NewGuid()));
    }
}
