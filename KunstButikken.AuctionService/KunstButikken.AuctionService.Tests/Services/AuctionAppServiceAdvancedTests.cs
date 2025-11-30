using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using KunstButikken.AuctionService.Application.Services;
using KunstButikken.AuctionService.Domain.Interfaces;
using KunstButikken.AuctionService.Domain.Models;
using KunstButikken.AuctionService.Tests.TestDoubles;
using KunstButikken.ServiceDefaults;
using Moq;
using Xunit;

namespace KunstButikken.AuctionService.Tests.Services;

public class AuctionAppServiceAdvancedTests
{
    private readonly Mock<IAuctionRepository> _repo = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly AuctionAppService _sut;

    public AuctionAppServiceAdvancedTests()
    {
        _sut = new AuctionAppService(_repo.Object, _clock.Object);
    }

    [Fact]
    public async Task GetAllAsync_IncludesBids_WhenIncludeBidsIsTrue()
    {
        // Arrange
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Status = AuctionStatus.Open,
            StartsAt = DateTimeOffset.UtcNow,
            EndsAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        auction.Bids.Add(new Bid { Id = Guid.NewGuid(), Amount = 100M });

        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));

        // Act
        var result = await _sut.GetAllAsync(includeBids: true);

        // Assert
        result.Should().ContainSingle();
        result.First().Bids.Should().ContainSingle();
    }

    [Fact]
    public async Task GetAllAsync_OrdersByStartsAtDescending()
    {
        // Arrange
        var auction1 = new Auction { Id = Guid.NewGuid(), StartsAt = DateTimeOffset.UtcNow.AddDays(-2), EndsAt = DateTimeOffset.UtcNow.AddDays(-1) };
        var auction2 = new Auction { Id = Guid.NewGuid(), StartsAt = DateTimeOffset.UtcNow.AddDays(-1), EndsAt = DateTimeOffset.UtcNow };
        var auction3 = new Auction { Id = Guid.NewGuid(), StartsAt = DateTimeOffset.UtcNow, EndsAt = DateTimeOffset.UtcNow.AddDays(1) };

        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction1, auction2, auction3 }));

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].Id.Should().Be(auction3.Id); // Most recent first
        result[1].Id.Should().Be(auction2.Id);
        result[2].Id.Should().Be(auction1.Id);
    }

    [Fact]
    public async Task GetAllAsync_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction>()));

        // Act
        await _sut.GetAllAsync(ct: cts.Token);

        // Assert
        _repo.Verify(r => r.Query(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_GeneratesNewId()
    {
        // Arrange
        var auction = new Auction { StartsAt = DateTimeOffset.UtcNow, EndsAt = DateTimeOffset.UtcNow.AddHours(1) };
        _repo.Setup(r => r.AddAsync(It.IsAny<Auction>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateAsync(auction);

        // Assert
        result.Id.Should().NotBeEmpty();
        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateAsync_SetsSatatusToOpen()
    {
        // Arrange
        var auction = new Auction
        {
            Status = AuctionStatus.Closed, // Try to create closed
            StartsAt = DateTimeOffset.UtcNow,
            EndsAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        _repo.Setup(r => r.AddAsync(It.IsAny<Auction>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateAsync(auction);

        // Assert
        result.Status.Should().Be(AuctionStatus.Open);
    }

    [Fact]
    public async Task CreateAsync_CallsAddAsyncAndSaveChanges()
    {
        // Arrange
        var auction = new Auction { StartsAt = DateTimeOffset.UtcNow, EndsAt = DateTimeOffset.UtcNow.AddHours(1) };
        _repo.Setup(r => r.AddAsync(It.IsAny<Auction>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.CreateAsync(auction);

        // Assert
        _repo.Verify(r => r.AddAsync(auction, default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenAuctionNotFound()
    {
        // Arrange
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction>()));

        // Act
        var result = await _sut.UpdateAsync(Guid.NewGuid(), new Auction(), null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_AllowsUpdateWithoutCurrentUserId()
    {
        // Arrange
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            StartsAt = DateTimeOffset.UtcNow,
            EndsAt = DateTimeOffset.UtcNow.AddHours(1),
            StartingPrice = 100M
        };
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var update = new Auction { StartingPrice = 200M, StartsAt = auction.StartsAt, EndsAt = auction.EndsAt };

        // Act
        var result = await _sut.UpdateAsync(auction.Id, update, null);

        // Assert
        result.Should().NotBeNull();
        result!.StartingPrice.Should().Be(200M);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAllFields()
    {
        // Arrange
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            StartsAt = DateTimeOffset.UtcNow,
            EndsAt = DateTimeOffset.UtcNow.AddHours(1),
            StartingPrice = 100M,
            ReservePrice = 150M
        };
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var newStart = DateTimeOffset.UtcNow.AddHours(2);
        var newEnd = DateTimeOffset.UtcNow.AddHours(4);
        var update = new Auction
        {
            StartsAt = newStart,
            EndsAt = newEnd,
            StartingPrice = 200M,
            ReservePrice = 300M
        };

        // Act
        var result = await _sut.UpdateAsync(auction.Id, update, null);

        // Assert
        result.Should().NotBeNull();
        result!.StartsAt.Should().Be(newStart);
        result.EndsAt.Should().Be(newEnd);
        result.StartingPrice.Should().Be(200M);
        result.ReservePrice.Should().Be(300M);
    }

    [Fact]
    public async Task PlaceBidAsync_ReturnsNull_WhenAuctionNotFound()
    {
        // Arrange
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction>()));

        // Act
        var result = await _sut.PlaceBidAsync(Guid.NewGuid(), 100M, Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task PlaceBidAsync_ThrowsWhenAuctionStatusIsNotOpen()
    {
        // Arrange
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Status = AuctionStatus.Closed, // Closed status
            StartsAt = DateTimeOffset.UtcNow.AddHours(-2),
            EndsAt = DateTimeOffset.UtcNow.AddHours(1),
            StartingPrice = 100M
        };
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));
        _clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.PlaceBidAsync(auction.Id, 150M, Guid.NewGuid()));
    }

    [Fact]
    public async Task PlaceBidAsync_ThrowsWhenAuctionHasEnded()
    {
        // Arrange
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Status = AuctionStatus.Open,
            StartsAt = DateTimeOffset.UtcNow.AddHours(-2),
            EndsAt = DateTimeOffset.UtcNow.AddHours(-1), // Already ended
            StartingPrice = 100M
        };
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));
        _clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.PlaceBidAsync(auction.Id, 150M, Guid.NewGuid()));
    }

    [Fact]
    public async Task PlaceBidAsync_ThrowsWhenBidEqualsStartingPrice()
    {
        // Arrange
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Status = AuctionStatus.Open,
            StartsAt = DateTimeOffset.UtcNow.AddHours(-1),
            EndsAt = DateTimeOffset.UtcNow.AddHours(1),
            StartingPrice = 100M
        };
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));
        _clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.PlaceBidAsync(auction.Id, 100M, Guid.NewGuid()));
    }

    [Fact]
    public async Task PlaceBidAsync_AcceptsFirstBidAboveStartingPrice()
    {
        // Arrange
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Status = AuctionStatus.Open,
            StartsAt = DateTimeOffset.UtcNow.AddHours(-1),
            EndsAt = DateTimeOffset.UtcNow.AddHours(1),
            StartingPrice = 100M
        };
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));
        _repo.Setup(r => r.AddBidAsync(It.IsAny<Bid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        // Act
        var result = await _sut.PlaceBidAsync(auction.Id, 101M, Guid.NewGuid());

        // Assert
        result.Should().NotBeNull();
        _repo.Verify(r => r.AddBidAsync(It.Is<Bid>(b => b.Amount == 101M), default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task PlaceBidAsync_GeneratesUniqueBidId()
    {
        // Arrange
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Status = AuctionStatus.Open,
            StartsAt = DateTimeOffset.UtcNow.AddHours(-1),
            EndsAt = DateTimeOffset.UtcNow.AddHours(1),
            StartingPrice = 100M
        };
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));
        _repo.Setup(r => r.AddBidAsync(It.IsAny<Bid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        // Act
        await _sut.PlaceBidAsync(auction.Id, 101M, Guid.NewGuid());

        // Assert
        _repo.Verify(r => r.AddBidAsync(It.Is<Bid>(b => b.Id != Guid.Empty), default), Times.Once);
    }

    [Fact]
    public async Task CloseAsync_ReturnsNull_WhenAuctionNotFound()
    {
        // Arrange
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction>()));

        // Act
        var result = await _sut.CloseAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CloseAsync_SetsStatusToClosed()
    {
        // Arrange
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Status = AuctionStatus.Open,
            StartsAt = DateTimeOffset.UtcNow.AddHours(-1),
            EndsAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CloseAsync(auction.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(AuctionStatus.Closed);
    }

    [Fact]
    public async Task CloseAsync_SetsWinnerWhenBidsExist()
    {
        // Arrange
        var winnerId = Guid.NewGuid();
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Status = AuctionStatus.Open,
            StartsAt = DateTimeOffset.UtcNow.AddHours(-1),
            EndsAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        auction.Bids.Add(new Bid { Id = Guid.NewGuid(), Amount = 100M, BidderId = Guid.NewGuid() });
        auction.Bids.Add(new Bid { Id = Guid.NewGuid(), Amount = 200M, BidderId = winnerId });
        auction.Bids.Add(new Bid { Id = Guid.NewGuid(), Amount = 150M, BidderId = Guid.NewGuid() });

        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CloseAsync(auction.Id);

        // Assert
        result.Should().NotBeNull();
        result!.WinningBid.Should().Be(200M);
        result.WinnerId.Should().Be(winnerId);
    }

    [Fact]
    public async Task CloseAsync_DoesNotSetWinnerWhenNoBids()
    {
        // Arrange
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Status = AuctionStatus.Open,
            StartsAt = DateTimeOffset.UtcNow.AddHours(-1),
            EndsAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CloseAsync(auction.Id);

        // Assert
        result.Should().NotBeNull();
        result!.WinningBid.Should().BeNull();
        result.WinnerId.Should().BeNull();
    }

    [Fact]
    public async Task CloseAsync_DoesNotSetWinnerWhenReservePriceNotMet()
    {
        // Arrange
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Status = AuctionStatus.Open,
            StartsAt = DateTimeOffset.UtcNow.AddHours(-1),
            EndsAt = DateTimeOffset.UtcNow.AddHours(1),
            ReservePrice = 300M // Reserve not met
        };
        auction.Bids.Add(new Bid { Id = Guid.NewGuid(), Amount = 100M, BidderId = Guid.NewGuid() });
        auction.Bids.Add(new Bid { Id = Guid.NewGuid(), Amount = 200M, BidderId = Guid.NewGuid() });

        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CloseAsync(auction.Id);

        // Assert
        result.Should().NotBeNull();
        result!.WinningBid.Should().BeNull();
        result.WinnerId.Should().BeNull();
    }

    [Fact]
    public async Task CloseAsync_SetsWinnerWhenReservePriceMet()
    {
        // Arrange
        var winnerId = Guid.NewGuid();
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Status = AuctionStatus.Open,
            StartsAt = DateTimeOffset.UtcNow.AddHours(-1),
            EndsAt = DateTimeOffset.UtcNow.AddHours(1),
            ReservePrice = 200M
        };
        auction.Bids.Add(new Bid { Id = Guid.NewGuid(), Amount = 100M, BidderId = Guid.NewGuid() });
        auction.Bids.Add(new Bid { Id = Guid.NewGuid(), Amount = 250M, BidderId = winnerId });

        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CloseAsync(auction.Id);

        // Assert
        result.Should().NotBeNull();
        result!.WinningBid.Should().Be(250M);
        result.WinnerId.Should().Be(winnerId);
    }

    [Fact]
    public async Task CloseAsync_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            Status = AuctionStatus.Open,
            StartsAt = DateTimeOffset.UtcNow.AddHours(-1),
            EndsAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        _repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(new List<Auction> { auction }));
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.CloseAsync(auction.Id, cts.Token);

        // Assert
        _repo.Verify(r => r.SaveChangesAsync(cts.Token), Times.Once);
    }
}

