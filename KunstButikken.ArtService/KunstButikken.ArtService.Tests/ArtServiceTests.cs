using System.IO;

using KunstButikken.ArtService.Domain.Models;
using KunstButikken.ArtService.Tests.Builders;
using KunstButikken.ArtService.Tests.Fakes;
using KunstButikken.IntegrationEvents.Contracts.Events;
using KunstButikken.ServiceDefaults;

using Microsoft.Extensions.Logging.Abstractions;

using ArtServiceApp = KunstButikken.ArtService.Application.Services.ArtService;

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

    [Fact]
    public async Task UpdateAsync_UpdatesArtAndPublishesEvent()
    {
        var art = new ArtBuilder().Build();
        _repo.Seed(art);

        var update = new ArtBuilder()
            .WithTitleEn("New Title")
            .WithDescriptionEn("New Description")
            .Build();

        await _sut.UpdateAsync(art.Id, update);

        var updatedArt = await _repo.FindAsync(art.Id);
        Assert.NotNull(updatedArt);
        Assert.Equal("New Title", updatedArt.TitleEn);
        Assert.Equal("New Description", updatedArt.DescriptionEn);
        Assert.IsType<ArtUpdatedIntegrationEvent>(_eventBus.PublishedEvent);
    }

    [Fact]
    public async Task DeleteAsync_RemovesArtAndPublishesEvent()
    {
        var art = new ArtBuilder().Build();
        _repo.Seed(art);

        await _sut.DeleteAsync(art.Id);

        var deletedArt = await _repo.FindAsync(art.Id);
        Assert.Null(deletedArt);
        Assert.IsType<ArtDeletedIntegrationEvent>(_eventBus.PublishedEvent);
    }

    [Fact]
    public async Task GetUnverifiedAsync_ReturnsOnlyUnverified()
    {
        _repo.Seed(
            new ArtBuilder().Verified().Build(),
            new ArtBuilder().Build());

        var result = await _sut.GetUnverifiedAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByStatus()
    {
        _repo.Seed(
            new ArtBuilder().WithStatus(ArtStatus.Draft).Build(),
            new ArtBuilder().WithStatus(ArtStatus.Published).Verified().Build());

        var result = await _sut.GetAllAsync(status: ArtStatus.Draft);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByFeatured()
    {
        _repo.Seed(
            new ArtBuilder().WithStatus(ArtStatus.Published).Featured().Verified().Build(),
            new ArtBuilder().WithStatus(ArtStatus.Published).Verified().Build());

        var result = await _sut.GetAllAsync(featured: true);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllAsync_DefaultsToPublishedAndVerified()
    {
        _repo.Seed(
            new ArtBuilder().WithStatus(ArtStatus.Published).Verified().Build(),
            new ArtBuilder().WithStatus(ArtStatus.Published).Build(),
            new ArtBuilder().WithStatus(ArtStatus.Draft).Verified().Build());

        var result = await _sut.GetAllAsync();

        // Only the published and verified art should be returned
        Assert.Single(result);
        Assert.All(result, a => Assert.Equal(ArtStatus.Published, a.Status));
    }
}
