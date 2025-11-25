using System.ComponentModel.DataAnnotations;

namespace KunstButikken.AuctionService.Domain.Models;

public class Art
{
    [Key] public Guid Id { get; set; }

    public required string Title { get; set; }
    public required string Artist { get; set; }
    public decimal Price { get; set; }
    public required string SellerId { get; set; }
}
