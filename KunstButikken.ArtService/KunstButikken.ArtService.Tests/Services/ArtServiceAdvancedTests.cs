using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using KunstButikken.ArtService.Domain.Models;
using KunstButikken.ArtService.Tests.Builders;
using KunstButikken.ArtService.Tests.Fakes;
using KunstButikken.IntegrationEvents.Contracts.Events;
using KunstButikken.ServiceDefaults;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ArtServiceApp = KunstButikken.ArtService.Application.Services.ArtService;

namespace KunstButikken.ArtService.Tests.Services;

public class ArtServiceAdvancedTests
{
    private readonly FakeArtRepository _repo = new();
    private readonly FakeBlobStorage _blob = new();
    private readonly FakeEventBus _eventBus = new();
    private readonly IDateTimeProvider _clock = new SystemDateTimeProvider();
    private readonly ArtServiceApp _sut;

    public ArtServiceAdvancedTests()
    {
        _sut = new ArtServiceApp(
            _repo,
            _blob,
            _eventBus,
            _clock,
            NullLogger<ArtServiceApp>.Instance);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyPublishedVerified_WhenNoFiltersProvided()
    {
        _repo.Seed(
            new ArtBuilder().WithStatus(ArtStatus.Published).Verified().Build(),
            new ArtBuilder().WithStatus(ArtStatus.Draft).Verified().Build(),
            new ArtBuilder().WithStatus(ArtStatus.Published).Verified(false).Build(),
            new ArtBuilder().WithStatus(ArtStatus.Published).Verified().Build());

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result.All(a => a.Status == ArtStatus.Published && a.IsVerified).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_FiltersByStatus_WhenStatusProvided()
    {
        _repo.Seed(
            new ArtBuilder().WithStatus(ArtStatus.Published).Verified().Build(),
            new ArtBuilder().WithStatus(ArtStatus.Draft).Build(),
            new ArtBuilder().WithStatus(ArtStatus.Published).Verified().Build());

        var result = await _sut.GetAllAsync(status: ArtStatus.Draft);

        result.Should().ContainSingle();
        result.First().Status.Should().Be(ArtStatus.Draft);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByFeatured_WhenFeaturedProvided()
    {
        _repo.Seed(
            new ArtBuilder().WithStatus(ArtStatus.Published).Featured().Build(),
            new ArtBuilder().WithStatus(ArtStatus.Published).Build(),
            new ArtBuilder().WithStatus(ArtStatus.Published).Featured().Build());

        var result = await _sut.GetAllAsync(status: ArtStatus.Published, featured: true);

        result.Should().HaveCount(2);
        result.All(a => a.IsFeatured).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_OrdersByCreatedAtDescending()
    {
        var art1 = new ArtBuilder().WithStatus(ArtStatus.Published).Verified().Build();
        art1.CreatedAt = DateTimeOffset.UtcNow.AddDays(-3);

        var art2 = new ArtBuilder().WithStatus(ArtStatus.Published).Verified().Build();
        art2.CreatedAt = DateTimeOffset.UtcNow.AddDays(-1);

        var art3 = new ArtBuilder().WithStatus(ArtStatus.Published).Verified().Build();
        art3.CreatedAt = DateTimeOffset.UtcNow;

        _repo.Seed(art1, art2, art3);

        var result = (await _sut.GetAllAsync()).ToList();

        result.Should().HaveCount(3);
        result[0].Id.Should().Be(art3.Id); // Most recent first
        result[1].Id.Should().Be(art2.Id);
        result[2].Id.Should().Be(art1.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsArt_WhenExists()
    {
        var art = new ArtBuilder().Build();
        _repo.Seed(art);

        var result = await _sut.GetByIdAsync(art.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(art.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_GeneratesNewId()
    {
        var art = new ArtBuilder().Build();

        var result = await _sut.CreateAsync(art);

        result.Id.Should().NotBeEmpty();
        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateAsync_SetsStatusToDraft()
    {
        var art = new ArtBuilder().WithStatus(ArtStatus.Published).Build();

        var result = await _sut.CreateAsync(art);

        result.Status.Should().Be(ArtStatus.Draft);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtTimestamp()
    {
        var beforeCreate = DateTimeOffset.UtcNow.AddSeconds(-1);
        var art = new ArtBuilder().Build();

        var result = await _sut.CreateAsync(art);

        result.CreatedAt.Should().BeAfter(beforeCreate);
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateAsync_PublishesArtCreatedEvent()
    {
        var art = new ArtBuilder()
            .WithTitleEn("Test Art")
            .WithArtist("Test Artist")
            .Build();

        await _sut.CreateAsync(art);

        _eventBus.PublishedEvent.Should().BeOfType<ArtCreatedIntegrationEvent>();
        var evt = (ArtCreatedIntegrationEvent)_eventBus.PublishedEvent!;
        evt.TitleEn.Should().Be("Test Art");
        evt.Artist.Should().Be("Test Artist");
    }

    [Fact]
    public async Task UpdateAsync_ThrowsKeyNotFoundException_WhenArtNotFound()
    {
        var update = new ArtBuilder().Build();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _sut.UpdateAsync(Guid.NewGuid(), update));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTitleEn()
    {
        var art = new ArtBuilder().WithTitleEn("Original Title").Build();
        _repo.Seed(art);

        var update = new ArtBuilder().WithTitleEn("Updated Title").Build();
        await _sut.UpdateAsync(art.Id, update);

        var updated = await _repo.FindAsync(art.Id);
        updated!.TitleEn.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTitleNb()
    {
        var art = new ArtBuilder().WithTitleNb("Opprinnelig Tittel").Build();
        _repo.Seed(art);

        var update = new ArtBuilder().WithTitleNb("Oppdatert Tittel").Build();
        await _sut.UpdateAsync(art.Id, update);

        var updated = await _repo.FindAsync(art.Id);
        updated!.TitleNb.Should().Be("Oppdatert Tittel");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesDescriptions()
    {
        var art = new ArtBuilder().Build();
        _repo.Seed(art);

        var update = new ArtBuilder()
            .WithDescriptionEn("New English Description")
            .WithDescriptionNb("Ny Norsk Beskrivelse")
            .Build();
        await _sut.UpdateAsync(art.Id, update);

        var updated = await _repo.FindAsync(art.Id);
        updated!.DescriptionEn.Should().Be("New English Description");
        updated.DescriptionNb.Should().Be("Ny Norsk Beskrivelse");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesPrice()
    {
        var art = new ArtBuilder().WithPrice(100M).Build();
        _repo.Seed(art);

        var update = new ArtBuilder().WithPrice(200M).Build();
        await _sut.UpdateAsync(art.Id, update);

        var updated = await _repo.FindAsync(art.Id);
        updated!.Price.Should().Be(200M);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesArtist()
    {
        var art = new ArtBuilder().WithArtist("Original Artist").Build();
        _repo.Seed(art);

        var update = new ArtBuilder().WithArtist("Updated Artist").Build();
        await _sut.UpdateAsync(art.Id, update);

        var updated = await _repo.FindAsync(art.Id);
        updated!.Artist.Should().Be("Updated Artist");
    }

    [Fact]
    public async Task UpdateAsync_PublishesArtUpdatedEvent()
    {
        var art = new ArtBuilder().Build();
        _repo.Seed(art);

        var update = new ArtBuilder().WithTitleEn("Updated").Build();
        await _sut.UpdateAsync(art.Id, update);

        _eventBus.PublishedEvent.Should().BeOfType<ArtUpdatedIntegrationEvent>();
        var evt = (ArtUpdatedIntegrationEvent)_eventBus.PublishedEvent!;
        evt.Id.Should().Be(art.Id);
    }

    [Fact]
    public async Task DeleteAsync_RemovesArtFromRepository()
    {
        var art = new ArtBuilder().Build();
        _repo.Seed(art);

        await _sut.DeleteAsync(art.Id);

        var deleted = await _repo.FindAsync(art.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DoesNotThrow_WhenArtNotFound()
    {
        await _sut.DeleteAsync(Guid.NewGuid());
        // Should not throw
    }

    [Fact]
    public async Task DeleteAsync_PublishesArtDeletedEvent_WhenArtExists()
    {
        var art = new ArtBuilder().Build();
        _repo.Seed(art);

        await _sut.DeleteAsync(art.Id);

        _eventBus.PublishedEvent.Should().BeOfType<ArtDeletedIntegrationEvent>();
        var evt = (ArtDeletedIntegrationEvent)_eventBus.PublishedEvent!;
        evt.ArtId.Should().Be(art.Id);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotPublishEvent_WhenArtNotFound()
    {
        await _sut.DeleteAsync(Guid.NewGuid());

        _eventBus.PublishedEvent.Should().BeNull();
    }

    [Fact]
    public async Task UploadImageAsync_ThrowsKeyNotFoundException_WhenArtNotFound()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _sut.UploadImageAsync(Guid.NewGuid(), stream, "test.png", "image/png"));
    }

    [Fact]
    public async Task UploadImageAsync_UpdatesArtImageUrl()
    {
        var art = new ArtBuilder().Build();
        _repo.Seed(art);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var response = await _sut.UploadImageAsync(art.Id, stream, "test.png", "image/png");

        var updated = await _repo.FindAsync(art.Id);
        updated!.ImageUrl.Should().NotBeNull();
        updated.ImageUrl.Should().Be(response.ImageUrl);
    }

    [Fact]
    public async Task UploadImageAsync_UsesCorrectFileExtension()
    {
        var art = new ArtBuilder().Build();
        _repo.Seed(art);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        await _sut.UploadImageAsync(art.Id, stream, "artwork.jpg", "image/jpeg");

        _blob.LastBlobName.Should().EndWith(".jpg");
    }

    [Fact]
    public async Task UploadImageAsync_IncludesArtIdInBlobPath()
    {
        var art = new ArtBuilder().Build();
        _repo.Seed(art);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        await _sut.UploadImageAsync(art.Id, stream, "test.png", "image/png");

        _blob.LastBlobName.Should().StartWith(art.Id.ToString());
    }

    [Fact]
    public async Task GetFeaturedAsync_ReturnsOnlyFeatured()
    {
        _repo.Seed(
            new ArtBuilder().WithStatus(ArtStatus.Published).Featured().Verified().Build(),
            new ArtBuilder().WithStatus(ArtStatus.Published).Verified().Build(),
            new ArtBuilder().WithStatus(ArtStatus.Published).Featured().Verified().Build());

        var result = await _sut.GetFeaturedAsync(10);

        result.Should().HaveCount(2);
        result.All(a => a.IsFeatured).Should().BeTrue();
    }

    [Fact]
    public async Task GetFeaturedAsync_ReturnsOnlyVerified()
    {
        _repo.Seed(
            new ArtBuilder().WithStatus(ArtStatus.Published).Featured().Verified().Build(),
            new ArtBuilder().WithStatus(ArtStatus.Published).Featured().Verified(false).Build());

        var result = await _sut.GetFeaturedAsync(10);

        result.Should().ContainSingle();
        result.First().IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task GetFeaturedAsync_ReturnsOnlyPublished()
    {
        _repo.Seed(
            new ArtBuilder().WithStatus(ArtStatus.Published).Featured().Verified().Build(),
            new ArtBuilder().WithStatus(ArtStatus.Draft).Featured().Verified().Build());

        var result = await _sut.GetFeaturedAsync(10);

        result.Should().ContainSingle();
        result.First().Status.Should().Be(ArtStatus.Published);
    }

    [Fact]
    public async Task GetFeaturedAsync_RespectsLimit()
    {
        for (int i = 0; i < 10; i++)
        {
            _repo.Seed(new ArtBuilder().WithStatus(ArtStatus.Published).Featured().Verified().Build());
        }

        var result = await _sut.GetFeaturedAsync(5);

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetFeaturedAsync_OrdersByCreatedAtDescending()
    {
        var art1 = new ArtBuilder().WithStatus(ArtStatus.Published).Featured().Verified().Build();
        art1.CreatedAt = DateTimeOffset.UtcNow.AddDays(-2);

        var art2 = new ArtBuilder().WithStatus(ArtStatus.Published).Featured().Verified().Build();
        art2.CreatedAt = DateTimeOffset.UtcNow.AddDays(-1);

        _repo.Seed(art1, art2);

        var result = (await _sut.GetFeaturedAsync(10)).ToList();

        result[0].Id.Should().Be(art2.Id);
        result[1].Id.Should().Be(art1.Id);
    }

    [Fact]
    public async Task GetUnverifiedAsync_ReturnsOnlyUnverified()
    {
        _repo.Seed(
            new ArtBuilder().Verified().Build(),
            new ArtBuilder().Verified(false).Build(),
            new ArtBuilder().Verified(false).Build());

        var result = await _sut.GetUnverifiedAsync();

        result.Should().HaveCount(2);
        result.All(a => !a.IsVerified).Should().BeTrue();
    }

    [Fact]
    public async Task GetUnverifiedAsync_OrdersByCreatedAtDescending()
    {
        var art1 = new ArtBuilder().Verified(false).Build();
        art1.CreatedAt = DateTimeOffset.UtcNow.AddDays(-2);

        var art2 = new ArtBuilder().Verified(false).Build();
        art2.CreatedAt = DateTimeOffset.UtcNow.AddDays(-1);

        _repo.Seed(art1, art2);

        var result = (await _sut.GetUnverifiedAsync()).ToList();

        result[0].Id.Should().Be(art2.Id);
        result[1].Id.Should().Be(art1.Id);
    }

    [Fact]
    public async Task GetUnverifiedAsync_ReturnsEmptyList_WhenAllVerified()
    {
        _repo.Seed(
            new ArtBuilder().Verified().Build(),
            new ArtBuilder().Verified().Build());

        var result = await _sut.GetUnverifiedAsync();

        result.Should().BeEmpty();
    }
}

