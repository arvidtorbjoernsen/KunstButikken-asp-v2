using KunstButikken.AdminService.Domain.Models;

namespace KunstButikken.AdminService.Domain.Interfaces;

public interface IAdminRepository
{
    IQueryable<AdminLog> Query();
    Task AddAsync(AdminLog log, CancellationToken ct = default);
    Task<AdminLog?> FindAsync(Guid id, CancellationToken ct = default);
    Task RemoveAsync(AdminLog log, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
