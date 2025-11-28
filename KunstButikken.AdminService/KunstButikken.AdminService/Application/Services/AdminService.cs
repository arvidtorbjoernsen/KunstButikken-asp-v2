using KunstButikken.AdminService.Application.Interfaces;
using KunstButikken.AdminService.Domain.Interfaces;
using KunstButikken.AdminService.Domain.Models;
using KunstButikken.ServiceDefaults;

namespace KunstButikken.AdminService.Application.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _repo;
    private readonly IDateTimeProvider _clock;

    public AdminService(IAdminRepository repo, IDateTimeProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public async Task<AdminActionResponse> ApproveArtAsync(Guid id, string performedBy, CancellationToken ct = default)
    {
        var log = new AdminLog
        {
            Id = Guid.NewGuid(),
            CreatedAt = _clock.Now,
            Action = "approve",
            PerformedBy = performedBy,
            Details = id.ToString()
        };

        await _repo.AddAsync(log, ct).ConfigureAwait(false);
        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);

        return new AdminActionResponse { ArtId = id, Status = "approved" };
    }

    public async Task<AdminActionResponse> RejectArtAsync(Guid id, string reason, string performedBy, CancellationToken ct = default)
    {
        var log = new AdminLog
        {
            Id = Guid.NewGuid(),
            CreatedAt = _clock.Now,
            Action = "reject",
            PerformedBy = performedBy,
            Details = reason
        };

        await _repo.AddAsync(log, ct).ConfigureAwait(false);
        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);

        return new AdminActionResponse { ArtId = id, Status = "rejected", Reason = reason };
    }

    public Task<IEnumerable<AdminLog>> GetLogsAsync(CancellationToken ct = default)
    {
        var q = _repo.Query()
            .OrderByDescending(l => l.CreatedAt)
            .AsEnumerable();

        return Task.FromResult<IEnumerable<AdminLog>>(q);
    }
}
