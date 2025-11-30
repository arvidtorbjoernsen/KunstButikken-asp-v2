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

    public ArtBuilder WithTitleEn(string title)
    {
        _art.TitleEn = title;
        _art.TitleNb = title;
        return this;
    }

    public ArtBuilder WithTitleNb(string title)
    {
        _art.TitleNb = title;
        return this;
    }

    public ArtBuilder WithDescriptionEn(string description)
    {
        _art.DescriptionEn = description;
        _art.DescriptionNb = description;
        return this;
    }

    public ArtBuilder WithDescriptionNb(string description)
    {
        _art.DescriptionNb = description;
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

    public ArtBuilder WithArtist(string artist)
    {
        _art.Artist = artist;
        return this;
    }

    public ArtBuilder WithPrice(decimal price)
    {
        _art.Price = price;
        return this;
    }

    public Art Build() => _art;
}
