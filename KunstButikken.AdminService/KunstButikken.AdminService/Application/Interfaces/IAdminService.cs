using KunstButikken.AdminService.Domain.Models;

namespace KunstButikken.AdminService.Application.Interfaces;

public interface IAdminService
{
    Task<AdminActionResponse> ApproveArtAsync(Guid id, string performedBy, CancellationToken ct = default);
    Task<AdminActionResponse> RejectArtAsync(Guid id, string reason, string performedBy, CancellationToken ct = default);
    Task<IEnumerable<AdminLog>> GetLogsAsync(CancellationToken ct = default);
}
