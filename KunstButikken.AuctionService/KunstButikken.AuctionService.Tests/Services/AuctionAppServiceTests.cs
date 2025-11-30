using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using KunstButikken.AuctionService.Application.Services;
using KunstButikken.AuctionService.Domain.Interfaces;
using KunstButikken.AuctionService.Domain.Models;
using KunstButikken.ServiceDefaults;
using KunstButikken.AuctionService.Tests.Fixtures;
using KunstButikken.AuctionService.Tests.TestDoubles;
using Moq;
using Xunit;

namespace KunstButikken.AuctionService.Tests.Services;

public class AuctionAppServiceTests
{
    private readonly Mock<IAuctionRepository> _repo = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly AuctionAppService _service;

    public AuctionAppServiceTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        var auctions = new List<Auction>
        {
            AuctionFixture.CreateAuction(status: AuctionStatus.Open),
            AuctionFixture.CreateAuction(status: AuctionStatus.Closed)
        }.AsQueryable();

        var asyncAuctions = new TestAsyncEnumerable<Auction>(auctions);
        _repo.Setup(r => r.Query()).Returns(asyncAuctions);
        _service = new AuctionAppService(_repo.Object, _clock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAuctions()
    {
        var result = await _service.GetAllAsync();
        result.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByStatusWhenProvided()
    {
        var result = await _service.GetAllAsync(AuctionStatus.Open);
        result.Should().ContainSingle(a => a.Status == AuctionStatus.Open);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAuction_WhenFound()
    {
        var auction = AuctionFixture.CreateAuction();
        auction.Bids.Add(new Bid
        {
            Id = Guid.NewGuid(),
            AuctionId = auction.Id,
            BidderId = Guid.NewGuid(),
            Amount = 200,
            PlacedAt = DateTimeOffset.UtcNow
        });

        _repo.SetupQuery(auction);

        var result = await _service.GetByIdAsync(auction.Id, includeBids: true);

        result.Should().NotBeNull();
        result!.Bids.Should().ContainSingle();
        result.Id.Should().Be(auction.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenMissing()
    {
        _repo.SetupQuery();

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAuction()
    {
        var auction = AuctionFixture.CreateAuction();
        _repo.Setup(r => r.AddAsync(auction, default)).Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(auction);

        result.Should().NotBeNull();
        result.Status.Should().Be(AuctionStatus.Open);
        _repo.Verify(r => r.AddAsync(auction, default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenAuctionMissing()
    {
        _repo.SetupQuery();

        var result = await _service.UpdateAsync(Guid.NewGuid(), AuctionFixture.CreateAuction(), Guid.NewGuid());

        result.Should().BeNull();
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAuction_WhenNoBids()
    {
        var auctionId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var auction = AuctionFixture.CreateAuction(auctionId);
        auction.SellerId = sellerId;

        _repo.SetupQuery(auction);

        var updatedAuction = new Auction
        {
            StartsAt = DateTimeOffset.UtcNow.AddDays(1),
            EndsAt = DateTimeOffset.UtcNow.AddDays(2),
            StartingPrice = 200,
            ReservePrice = 300
        };

        var result = await _service.UpdateAsync(auctionId, updatedAuction, sellerId);

        result.Should().NotBeNull();
        result!.StartsAt.Should().Be(updatedAuction.StartsAt);
        result.EndsAt.Should().Be(updatedAuction.EndsAt);
        result.StartingPrice.Should().Be(200);
        result.ReservePrice.Should().Be(300);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenBidsExist()
    {
        var auction = AuctionFixture.CreateAuction();
        auction.SellerId = Guid.NewGuid();
        auction.Bids.Add(new Bid
        {
            Id = Guid.NewGuid(),
            AuctionId = auction.Id,
            BidderId = Guid.NewGuid(),
            Amount = 500,
            PlacedAt = DateTimeOffset.UtcNow
        });

        _repo.SetupQuery(auction);

        await _service.Invoking(s => s.UpdateAsync(auction.Id, new Auction(), auction.SellerId))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PlaceBidAsync_PlacesBid_WhenValid()
    {
        var auctionId = Guid.NewGuid();
        var auction = AuctionFixture.CreateAuction(auctionId);
        auction.Status = AuctionStatus.Open;
        auction.EndsAt = DateTimeOffset.UtcNow.AddHours(1);

        _repo.SetupQuery(auction);

        var result = await _service.PlaceBidAsync(auctionId, 150, Guid.NewGuid());

        result.Should().NotBeNull();
        _repo.Verify(r => r.AddBidAsync(It.IsAny<Bid>(), default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task PlaceBidAsync_ThrowsWhenAuctionClosed()
    {
        var auction = AuctionFixture.CreateAuction();
        auction.Status = AuctionStatus.Closed;

        _repo.SetupQuery(auction);

        await _service.Invoking(s => s.PlaceBidAsync(auction.Id, auction.StartingPrice + 1, Guid.NewGuid()))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PlaceBidAsync_ReturnsNull_WhenAuctionMissing()
    {
        _repo.SetupQuery();

        var result = await _service.PlaceBidAsync(Guid.NewGuid(), 200, Guid.NewGuid());

        result.Should().BeNull();
        _repo.Verify(r => r.AddBidAsync(It.IsAny<Bid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CloseAsync_ClosesAuction_AndSetsWinner_WhenReserveMet()
    {
        var auctionId = Guid.NewGuid();
        var auction = AuctionFixture.CreateAuction(auctionId);
        auction.ReservePrice = 200;
        var winnerId = Guid.NewGuid();
        auction.Bids.Add(new Bid { BidderId = winnerId, Amount = 250 });

        _repo.SetupQuery(auction);

        var result = await _service.CloseAsync(auctionId);

        result.Should().NotBeNull();
        result!.Status.Should().Be(AuctionStatus.Closed);
        result.WinningBid.Should().Be(250);
        result.WinnerId.Should().Be(winnerId);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CloseAsync_ClosesAuction_AndNoWinner_WhenReserveNotMet()
    {
        var auctionId = Guid.NewGuid();
        var auction = AuctionFixture.CreateAuction(auctionId);
        auction.ReservePrice = 300;
        auction.Bids.Add(new Bid { BidderId = Guid.NewGuid(), Amount = 250 });

        _repo.SetupQuery(auction);

        var result = await _service.CloseAsync(auctionId);

        result.Should().NotBeNull();
        result!.Status.Should().Be(AuctionStatus.Closed);
        result.WinningBid.Should().BeNull();
        result.WinnerId.Should().BeNull();
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CloseAsync_ReturnsNull_WhenAuctionMissing()
    {
        _repo.SetupQuery();

        var result = await _service.CloseAsync(Guid.NewGuid());

        result.Should().BeNull();
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
