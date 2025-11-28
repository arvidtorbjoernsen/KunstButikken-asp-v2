using System.IO;
using ArtServiceApp = KunstButikken.ArtService.Application.Services.ArtService;
using KunstButikken.ArtService.Domain.Models;
using KunstButikken.ArtService.Tests.Builders;
using KunstButikken.ArtService.Tests.Fakes;
using KunstButikken.IntegrationEvents.Contracts.Events;
using KunstButikken.ServiceDefaults;
using Microsoft.Extensions.Logging.Abstractions;

namespace KunstButikken.ArtService.Tests;

public class ArtServiceTests
{
    private readonly FakeArtRepository _repo = new();
    private readonly FakeBlobStorage _blob = new();
    private readonly FakeEventBus _eventBus = new();
    private readonly IDateTimeProvider _clock = new SystemDateTimeProvider();
    private readonly ArtServiceApp _sut;

    public ArtServiceTests()
    {
        _sut = new ArtServiceApp(
            _repo,
            _blob,
            _eventBus,
            _clock,
            NullLogger<ArtServiceApp>.Instance);
    }

    [Fact]
    public async Task CreateAsync_PublishesCreatedEvent()
    {
        var art = new ArtBuilder().Build();

        var created = await _sut.CreateAsync(art);

        Assert.Equal(ArtStatus.Draft, created.Status);
        Assert.IsType<ArtCreatedIntegrationEvent>(_eventBus.PublishedEvent);
    }

    [Fact]
    public async Task GetFeatured_ReturnsOnlyFeaturedVerifiedPublished()
    {
        _repo.Seed(
            new ArtBuilder().WithStatus(ArtStatus.Published).Featured().Verified().Build(),
            new ArtBuilder().WithStatus(ArtStatus.Published).Verified().Build(),
            new ArtBuilder().WithStatus(ArtStatus.Draft).Featured().Verified().Build());

        var result = await _sut.GetFeaturedAsync(10);

        Assert.Single(result);
    }

    [Fact]
    public async Task UploadImage_UpdatesArt()
    {
        var art = new ArtBuilder().Build();
        _repo.Seed(art);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var response = await _sut.UploadImageAsync(art.Id, stream, "art.png", "image/png");

        Assert.NotNull(response.ImageUrl);
        Assert.Equal(response.ImageUrl, art.ImageUrl);
    }
}
