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

    [Fact]
    public async Task GetLogsAsync_ReturnsEmptyWhenNoLogs()
    {
        _repo.Setup(r => r.Query()).Returns(new List<AdminLog>().AsQueryable());

        var result = await _sut.GetLogsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ApproveArtAsync_UsesCorrectTimestamp()
    {
        var artId = Guid.NewGuid();
        var expectedTime = DateTimeOffset.Parse("2025-01-01T00:00:00Z");

        await _sut.ApproveArtAsync(artId, "admin");

        _repo.Verify(r => r.AddAsync(It.Is<AdminLog>(l => l.CreatedAt == expectedTime), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveArtAsync_GeneratesUniqueId()
    {
        var artId = Guid.NewGuid();
        Guid? capturedId = null;

        _repo.Setup(r => r.AddAsync(It.IsAny<AdminLog>(), It.IsAny<CancellationToken>()))
            .Callback<AdminLog, CancellationToken>((log, _) => capturedId = log.Id);

        await _sut.ApproveArtAsync(artId, "admin");

        Assert.NotNull(capturedId);
        Assert.NotEqual(Guid.Empty, capturedId.Value);
    }

    [Fact]
    public async Task RejectArtAsync_IncludesReasonInLog()
    {
        var artId = Guid.NewGuid();
        var reason = "Content violates guidelines";

        var result = await _sut.RejectArtAsync(artId, reason, "admin");

        _repo.Verify(r => r.AddAsync(It.Is<AdminLog>(l => l.Details == reason), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(reason, result.Reason);
    }

    [Fact]
    public async Task RejectArtAsync_SetsCorrectAction()
    {
        var artId = Guid.NewGuid();

        await _sut.RejectArtAsync(artId, "reason", "admin");

        _repo.Verify(r => r.AddAsync(It.Is<AdminLog>(l => l.Action == "reject"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveArtAsync_WithDifferentPerformers_RecordsCorrectly()
    {
        var artId = Guid.NewGuid();
        var performer1 = "admin1";
        var performer2 = "admin2";

        await _sut.ApproveArtAsync(artId, performer1);
        await _sut.ApproveArtAsync(artId, performer2);

        _repo.Verify(r => r.AddAsync(It.Is<AdminLog>(l => l.PerformedBy == performer1), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.AddAsync(It.Is<AdminLog>(l => l.PerformedBy == performer2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLogsAsync_WithMultipleActions_PreservesOrder()
    {
        var now = DateTimeOffset.Parse("2025-01-01T12:00:00Z");
        var logs = new List<AdminLog>
        {
            new() { Id = Guid.NewGuid(), Action = "approve", CreatedAt = now.AddHours(-3) },
            new() { Id = Guid.NewGuid(), Action = "reject", CreatedAt = now.AddHours(-2) },
            new() { Id = Guid.NewGuid(), Action = "approve", CreatedAt = now.AddHours(-1) }
        };
        _repo.Setup(r => r.Query()).Returns(logs.AsQueryable());

        var result = (await _sut.GetLogsAsync()).ToList();

        Assert.Equal(3, result.Count);
        Assert.True(result[0].CreatedAt > result[1].CreatedAt);
        Assert.True(result[1].CreatedAt > result[2].CreatedAt);
    }

    [Fact]
    public async Task ApproveArtAsync_SavesChangesOnce()
    {
        var artId = Guid.NewGuid();

        await _sut.ApproveArtAsync(artId, "admin");

        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectArtAsync_SavesChangesOnce()
    {
        var artId = Guid.NewGuid();

        await _sut.RejectArtAsync(artId, "reason", "admin");

        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveArtAsync_WithEmptyGuid_StillCreatesLog()
    {
        var emptyGuid = Guid.Empty;

        var result = await _sut.ApproveArtAsync(emptyGuid, "admin");

        Assert.Equal(emptyGuid, result.ArtId);
        _repo.Verify(r => r.AddAsync(It.IsAny<AdminLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectArtAsync_WithEmptyReason_StillCreatesLog()
    {
        var artId = Guid.NewGuid();

        var result = await _sut.RejectArtAsync(artId, string.Empty, "admin");

        Assert.Equal(string.Empty, result.Reason);
        _repo.Verify(r => r.AddAsync(It.Is<AdminLog>(l => l.Details == string.Empty), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveArtAsync_WithEmptyPerformer_StillCreatesLog()
    {
        var artId = Guid.NewGuid();

        await _sut.ApproveArtAsync(artId, string.Empty);

        _repo.Verify(r => r.AddAsync(It.Is<AdminLog>(l => l.PerformedBy == string.Empty), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLogsAsync_WithCancellationToken_PassesTokenCorrectly()
    {
        var cts = new CancellationTokenSource();
        _repo.Setup(r => r.Query()).Returns(new List<AdminLog>().AsQueryable());

        await _sut.GetLogsAsync(cts.Token);

        _repo.Verify(r => r.Query(), Times.Once);
    }

    [Fact]
    public async Task ApproveArtAsync_PassesCancellationToken()
    {
        var artId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        await _sut.ApproveArtAsync(artId, "admin", cts.Token);

        _repo.Verify(r => r.AddAsync(It.IsAny<AdminLog>(), cts.Token), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task RejectArtAsync_PassesCancellationToken()
    {
        var artId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        await _sut.RejectArtAsync(artId, "reason", "admin", cts.Token);

        _repo.Verify(r => r.AddAsync(It.IsAny<AdminLog>(), cts.Token), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task ApproveArtAsync_MultipleCallsGenerateUniqueIds()
    {
        var artId = Guid.NewGuid();
        var capturedIds = new List<Guid>();

        _repo.Setup(r => r.AddAsync(It.IsAny<AdminLog>(), It.IsAny<CancellationToken>()))
            .Callback<AdminLog, CancellationToken>((log, _) => capturedIds.Add(log.Id));

        await _sut.ApproveArtAsync(artId, "admin1");
        await _sut.ApproveArtAsync(artId, "admin2");
        await _sut.ApproveArtAsync(artId, "admin3");

        Assert.Equal(3, capturedIds.Count);
        Assert.Equal(3, capturedIds.Distinct().Count());
    }

    [Fact]
    public async Task GetLogsAsync_WithLargeDataset_ReturnsAllOrdered()
    {
        var logs = Enumerable.Range(0, 100)
            .Select(i => new AdminLog
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
            })
            .ToList();
        _repo.Setup(r => r.Query()).Returns(logs.AsQueryable());

        var result = (await _sut.GetLogsAsync()).ToList();

        Assert.Equal(100, result.Count);
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].CreatedAt >= result[i + 1].CreatedAt);
        }
    }
}
