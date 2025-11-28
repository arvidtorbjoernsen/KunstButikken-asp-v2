namespace KunstButikken.UserService.Application.Services;

using KunstButikken.UserService.Application.Interfaces;
using KunstButikken.UserService.Domain.Entities;
using KunstButikken.UserService.Domain.Interfaces;

public sealed class AdminProfileService : IAdminProfileService
{
    private readonly IUserRepository _userRepository;

    public AdminProfileService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public Task<IReadOnlyList<UserProfile>> GetPendingSellersAsync(CancellationToken ct = default)
    {
        return _userRepository.GetPendingSellersAsync(ct);
    }

    public Task<IReadOnlyList<UserProfile>> GetAdminsAsync(CancellationToken ct = default)
    {
        return _userRepository.GetAdminsAsync(ct);
    }

    public async Task ToggleAdminAsync(Guid userId, bool makeAdmin, CancellationToken ct = default)
    {
        var profile = await _userRepository.GetByUserIdAsync(userId, ct).ConfigureAwait(false)
                      ?? throw new InvalidOperationException("Profile not found");
        profile.IsAdmin = makeAdmin;
        await _userRepository.UpdateAsync(profile, ct).ConfigureAwait(false);
    }

    public async Task<UserProfile> MakeAdminByEmailAsync(string email, CancellationToken ct = default)
    {
        var profile = await _userRepository.GetByEmailAsync(email, ct).ConfigureAwait(false)
                      ?? throw new InvalidOperationException("User profile with this email was not found");
        profile.IsAdmin = true;
        await _userRepository.UpdateAsync(profile, ct).ConfigureAwait(false);
        return profile;
    }

    public async Task ToggleSellerVerificationAsync(Guid userId, bool verify, CancellationToken ct = default)
    {
        var profile = await _userRepository.GetByUserIdAsync(userId, ct).ConfigureAwait(false)
                      ?? throw new InvalidOperationException("Profile not found");
        if (verify)
        {
            if (!profile.IsSeller)
            {
                throw new InvalidOperationException("User is not a seller");
            }
            profile.IsSellerVerified = true;
        }
        else
        {
            profile.IsSellerVerified = false;
            profile.IsSeller = false;
        }

        await _userRepository.UpdateAsync(profile, ct).ConfigureAwait(false);
    }
}
