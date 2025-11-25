using KunstButikken.UserService.Models;

namespace KunstButikken.UserService.Data;

public class UserRepository : IUserRepository
{
    private readonly UserDbContext _db;

    public UserRepository(UserDbContext db) => _db = db;

    public IQueryable<UserProfile> Query() => _db.Profiles.AsQueryable();

    public async Task<UserProfile?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Profiles.FindAsync(new object[] { id }, cancellationToken).AsTask().ConfigureAwait(false);

    public Task AddAsync(UserProfile user, CancellationToken cancellationToken = default)
    {
        _db.Profiles.Add(user);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
