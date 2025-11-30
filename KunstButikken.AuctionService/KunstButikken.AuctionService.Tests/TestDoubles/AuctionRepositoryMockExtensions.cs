using System.Collections.Generic;
using System.Linq;
using System.Threading;
using KunstButikken.AuctionService.Domain.Interfaces;
using KunstButikken.AuctionService.Domain.Models;
using Moq;

namespace KunstButikken.AuctionService.Tests.TestDoubles;

internal static class AuctionRepositoryMockExtensions
{
    public static void SetupQuery(this Mock<IAuctionRepository> repo, params Auction[] auctions)
        => repo.SetupQuery(auctions.AsEnumerable());

    public static void SetupQuery(this Mock<IAuctionRepository> repo, IEnumerable<Auction> auctions)
    {
        var query = auctions.AsQueryable();
        repo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Auction>(query));
    }
}

