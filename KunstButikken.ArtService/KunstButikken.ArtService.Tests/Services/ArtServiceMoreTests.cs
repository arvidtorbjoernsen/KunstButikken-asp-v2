using System.IO;
using ArtServiceApp = KunstButikken.ArtService.Application.Services.ArtService;
using KunstButikken.ArtService.Domain.Models;
using KunstButikken.ArtService.Tests.Builders;
using KunstButikken.ArtService.Tests.Fakes;
using KunstButikken.ServiceDefaults;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

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
}
