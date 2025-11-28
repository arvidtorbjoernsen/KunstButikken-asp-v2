using ApplicationAdminService = KunstButikken.AdminService.Application.Services.AdminService;
using KunstButikken.AdminService.Domain.Interfaces;
using KunstButikken.AdminService.Domain.Models;
using KunstButikken.ServiceDefaults;
using Moq;
using Xunit;

namespace KunstButikken.AdminService.Tests.Application;

public class AdminServiceTests
{
    private readonly Mock<IAdminRepository> _repo = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly ApplicationAdminService _sut;

    public AdminServiceTests()
    {
        _clock.Setup(c => c.Now).Returns(DateTimeOffset.Parse("2025-01-01T00:00:00Z"));
        _sut = new ApplicationAdminService(_repo.Object, _clock.Object);
    }

    [Fact]
    public async Task ApproveArtAsync_AddsApprovalLogAndReturnsResponse()
    {
        var artId = Guid.NewGuid();

        var result = await _sut.ApproveArtAsync(artId, "admin");

        _repo.Verify(r => r.AddAsync(It.Is<Domain.Models.AdminLog>(l =>
            l.Action == "approve" &&
            l.Details == artId.ToString() &&
            l.PerformedBy == "admin" &&
            l.CreatedAt == DateTimeOffset.Parse("2025-01-01T00:00:00Z")), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(artId, result.ArtId);
        Assert.Equal("approved", result.Status);
    }

    [Fact]
    public async Task RejectArtAsync_AddsRejectionLogAndReturnsResponse()
    {
        var artId = Guid.NewGuid();

        var result = await _sut.RejectArtAsync(artId, "bad art", "admin");

        _repo.Verify(r => r.AddAsync(It.Is<Domain.Models.AdminLog>(l =>
            l.Action == "reject" &&
            l.Details == "bad art" &&
            l.PerformedBy == "admin"), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("rejected", result.Status);
        Assert.Equal("bad art", result.Reason);
    }

    [Fact]
    public async Task GetLogsAsync_ReturnsLogsOrderedByCreatedAtDesc()
    {
        var logs = new List<AdminLog>
        {
            new() { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow.AddDays(-2) },
            new() { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow }
        };
        _repo.Setup(r => r.Query()).Returns(logs.AsQueryable());

        var result = await _sut.GetLogsAsync();

        Assert.True(result.SequenceEqual(logs.OrderByDescending(l => l.CreatedAt)));
        _repo.Verify(r => r.Query(), Times.Once);
    }
}
