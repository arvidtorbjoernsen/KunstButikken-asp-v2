using KunstButikken.AdminService.Domain.Interfaces;
using KunstButikken.AdminService.Models;
using KunstButikken.AdminService.Infrastructure.Data;

namespace KunstButikken.AdminService.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly AdminDbContext _db;

    public AdminRepository(AdminDbContext db)
    {
        _db = db;
    }

    public IQueryable<AdminLog> Query() => _db.Logs.AsQueryable();

    public Task AddAsync(AdminLog log, CancellationToken ct = default) => _db.Logs.AddAsync(log, ct).AsTask();

    public Task<AdminLog?> FindAsync(Guid id, CancellationToken ct = default) => _db.Logs.FindAsync(new object[] { id }, ct).AsTask();

    public Task RemoveAsync(AdminLog log, CancellationToken ct = default)
    {
        _db.Logs.Remove(log);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
