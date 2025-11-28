using KunstButikken.ArtService.Domain.Models;

namespace KunstButikken.ArtService.Tests.Builders;

internal sealed class ArtBuilder
{
    private readonly Art _art = new()
    {
        Id = Guid.NewGuid(),
        TitleEn = "Test Art",
        TitleNb = "Test Art",
        DescriptionEn = "Description",
        DescriptionNb = "Description",
        Price = 123m,
        SellerId = Guid.NewGuid(),
        SellerDisplayName = "Seller",
        Artist = "Artist",
        ImageUrl = new Uri("https://example.com/art.jpg"),
        Status = ArtStatus.Draft,
        CreatedAt = DateTimeOffset.UtcNow,
        IsVerified = false,
        IsFeatured = false
    };

    public ArtBuilder WithStatus(ArtStatus status)
    {
        _art.Status = status;
        return this;
    }

    public ArtBuilder Featured(bool featured = true)
    {
        _art.IsFeatured = featured;
        return this;
    }

    public ArtBuilder Verified(bool verified = true)
    {
        _art.IsVerified = verified;
        return this;
    }

    public Art Build() => _art;
}

