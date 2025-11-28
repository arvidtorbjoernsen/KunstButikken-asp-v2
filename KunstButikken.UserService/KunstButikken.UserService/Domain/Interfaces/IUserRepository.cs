namespace KunstButikken.UserService.Domain.Interfaces;

using KunstButikken.UserService.Domain.Dtos;
using KunstButikken.UserService.Domain.Entities;

public interface IUserRepository
{
    Task<UserProfile?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UserProfile?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<UserProfile>> GetPendingSellersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UserProfile>> GetAdminsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UnverifiedSellerDto>> ListUnverifiedSellersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SellerDto>> GetSellersAsync(CancellationToken ct = default);
    Task<SellerDto?> GetSellerByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(UserProfile user, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserProfile user, CancellationToken cancellationToken = default);
}
