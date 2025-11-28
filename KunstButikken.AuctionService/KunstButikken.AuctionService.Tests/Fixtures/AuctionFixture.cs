using KunstButikken.AuctionService.Domain.Models;

namespace KunstButikken.AuctionService.Tests.Fixtures;

public static class AuctionFixture
{
    public static Auction CreateAuction(Guid? id = null, AuctionStatus status = AuctionStatus.Open)
    {
        return new Auction
        {
            Id = id ?? Guid.NewGuid(),
            ArtId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            SellerDisplayName = "Test Seller",
            StartsAt = DateTimeOffset.UtcNow.AddMinutes(-30),
            EndsAt = DateTimeOffset.UtcNow.AddHours(1),
            StartingPrice = 100,
            Status = status
        };
    }
}

