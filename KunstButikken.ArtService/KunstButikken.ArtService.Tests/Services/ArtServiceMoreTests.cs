using System.IO;

using KunstButikken.ArtService.Domain.Models;
using KunstButikken.ArtService.Tests.Builders;
using KunstButikken.ArtService.Tests.Fakes;
using KunstButikken.ServiceDefaults;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

using ArtServiceApp = KunstButikken.ArtService.Application.Services.ArtService;

namespace KunstButikken.ArtService.Tests.Services;

public class ArtServiceMoreTests
{
    private readonly FakeArtRepository _repo = new();
    private readonly FakeBlobStorage _blob = new();
    private readonly FakeEventBus _eventBus = new();
    private readonly ArtServiceApp _sut;

    public ArtServiceMoreTests()
    {
        _sut = new ArtServiceApp(
            _repo,
            _blob,
            _eventBus,
            new SystemDateTimeProvider(),
            NullLogger<ArtServiceApp>.Instance);
    }

    [Fact]
    public async Task CreateAsync_PublishesCreatedEventAndSetsDraft()
    {
        var art = new ArtBuilder().Build();

        var result = await _sut.CreateAsync(art);

        Assert.Equal(ArtStatus.Draft, result.Status);
        Assert.NotNull(_eventBus.PublishedEvent);
        Assert.Equal(art.TitleEn, ((KunstButikken.IntegrationEvents.Contracts.Events.ArtCreatedIntegrationEvent)_eventBus.PublishedEvent!).TitleEn);
    }

    [Fact]
    public async Task UploadImageAsync_ThrowsWhenArtMissing()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _sut.UploadImageAsync(Guid.NewGuid(), Stream.Null, "file.png", "image/png"));
    }

    [Fact]
    public async Task UploadImageAsync_SetsImageUrlOnArt()
    {
        var art = new ArtBuilder().Build();
        // ensure art is in repository
        await _sut.CreateAsync(art);

        var result = await _sut.UploadImageAsync(art.Id, Stream.Null, "file.png", "image/png");

        Assert.NotNull(result.ImageUrl);
        Assert.NotNull(_blob.LastUploaded);
        Assert.Equal(result.ImageUrl, art.ImageUrl);
    }

    [Fact]
    public async Task GetFeaturedAsync_ReturnsOnlyFeaturedVerifiedPublished_AndRespectsLimit()
    {
        var now = DateTimeOffset.UtcNow;
        var a1 = new ArtBuilder().WithTitleEn("One").Featured(true).Verified(true).WithStatus(ArtStatus.Published).Build();
        a1.CreatedAt = now.AddMinutes(-1);
        var a2 = new ArtBuilder().WithTitleEn("Two").Featured(true).Verified(true).WithStatus(ArtStatus.Published).Build();
        a2.CreatedAt = now;
        var a3 = new ArtBuilder().WithTitleEn("Three").Featured(false).Verified(true).WithStatus(ArtStatus.Published).Build();

        _repo.Seed(a1, a2, a3);

        var result = (await _sut.GetFeaturedAsync(1)).ToList();

        Assert.Single(result);
        // Should return the most recent featured item (a2)
        Assert.Equal("Two", result[0].TitleEn);
    }
}
