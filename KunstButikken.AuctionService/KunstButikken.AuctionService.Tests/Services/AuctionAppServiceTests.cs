using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KunstButikken.AuctionService.Application.Services;
using KunstButikken.AuctionService.Domain.Interfaces;
using KunstButikken.AuctionService.Domain.Models;
using KunstButikken.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Moq;
using KunstButikken.AuctionService.Tests.Fixtures;
using KunstButikken.AuctionService.Tests.TestDoubles;
using FluentAssertions;

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
        var result = await _service.GetAllAsync().ConfigureAwait(false);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByStatusWhenProvided()
    {
        var result = await _service.GetAllAsync(AuctionStatus.Open).ConfigureAwait(false);
        result.Should().ContainSingle(a => a.Status == AuctionStatus.Open);
    }
}
