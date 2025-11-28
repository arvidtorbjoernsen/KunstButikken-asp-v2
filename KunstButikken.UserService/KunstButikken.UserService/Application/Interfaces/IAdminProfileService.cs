namespace KunstButikken.UserService.Application.Interfaces;

using KunstButikken.UserService.Domain.Entities;

public interface IAdminProfileService
{
    Task<IReadOnlyList<UserProfile>> GetPendingSellersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UserProfile>> GetAdminsAsync(CancellationToken ct = default);
    Task ToggleAdminAsync(Guid userId, bool makeAdmin, CancellationToken ct = default);
    Task<UserProfile> MakeAdminByEmailAsync(string email, CancellationToken ct = default);
    Task ToggleSellerVerificationAsync(Guid userId, bool verify, CancellationToken ct = default);
}
