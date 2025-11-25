using System.Collections.ObjectModel;

namespace KunstButikken.AuctionService.Models;

public enum AuctionStatus
{
    Draft,
    Open,
    Closed
}

public class Auction
{
    public Guid Id { get; set; }
    public Guid ArtId { get; set; }
    public Guid SellerId { get; set; }
    public string SellerDisplayName { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public decimal StartingPrice { get; set; }
    public decimal? ReservePrice { get; set; }
    public decimal? WinningBid { get; set; }
    public Guid? WinnerId { get; set; }
    public AuctionStatus Status { get; set; } = AuctionStatus.Draft;
    public Collection<Bid> Bids { get; } = [];
}

public class Bid
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public Guid BidderId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset PlacedAt { get; set; }
}
