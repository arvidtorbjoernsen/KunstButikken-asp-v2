using KunstButikken.UserService.Models;

namespace KunstButikken.UserService.Data;

public interface IUserRepository
{
  IQueryable<UserProfile> Query();
  Task<UserProfile?> FindAsync(Guid id, CancellationToken cancellationToken = default);
  Task AddAsync(UserProfile user, CancellationToken cancellationToken = default);
  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}