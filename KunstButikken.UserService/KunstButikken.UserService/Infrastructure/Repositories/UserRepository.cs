namespace KunstButikken.UserService.Infrastructure.Repositories;

using KunstButikken.UserService.Domain.Entities;
using KunstButikken.UserService.Domain.Interfaces;
using KunstButikken.UserService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class UserRepository : IUserRepository
{
    private readonly UserDbContext _db;

    public UserRepository(UserDbContext db)
    {
        _db = db;
    }

    public IQueryable<UserProfile> Query() => _db.Profiles.AsQueryable();

    public async Task<UserProfile?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Profiles.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);

    public Task AddAsync(UserProfile user, CancellationToken cancellationToken = default)
    {
        return _db.Profiles.AddAsync(user, cancellationToken).AsTask();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
