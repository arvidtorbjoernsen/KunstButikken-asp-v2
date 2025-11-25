using System.Collections.ObjectModel;

namespace KunstButikken.AuctionService.Controllers;

public class SeedRequest
{
    public int Count { get; set; } = 5;
    public bool Force { get; set; }
}

public class SeedResult
{
    public int Inserted { get; set; }
    public int Skipped { get; set; }
    public Collection<string> Messages { get; } = [];
}

internal class ArtDto
{
    public Guid Id { get; set; }
    public decimal Price { get; set; }
    public Guid? SellerId { get; set; }
    public string? SellerDisplayName { get; set; }
}
