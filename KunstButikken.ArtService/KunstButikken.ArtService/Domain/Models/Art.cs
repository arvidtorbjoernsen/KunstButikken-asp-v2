namespace KunstButikken.ArtService.Domain.Models;

public enum ArtStatus
{
    Draft = 0,
    Published = 1,
    Sold = 2,
    Rejected = 3
}

public class Art
{
    public Guid Id { get; set; }

    // Multilingual fields
    public string TitleNb { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string DescriptionNb { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;

    public Uri ImageUrl { get; set; } = new("http://localhost");
    public decimal Price { get; set; }

    public Guid SellerId { get; set; }

    // The painting's credited artist (e.g. painter name)
    public string Artist { get; set; } = string.Empty;

    // Cached display name of the seller (so UI doesn't have to resolve user profiles for listings)
    public string SellerDisplayName { get; set; } = string.Empty;
    public ArtStatus Status { get; set; } = ArtStatus.Draft;
    public bool IsVerified { get; set; }
    public bool IsFeatured { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

