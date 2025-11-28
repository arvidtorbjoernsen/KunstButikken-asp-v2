namespace KunstButikken.UserService.Application.Services;

using KunstButikken.UserService.Application.Interfaces;
using KunstButikken.UserService.Domain.Interfaces;
using KunstButikken.UserService.Models;
using Microsoft.EntityFrameworkCore;

public sealed class AdminProfileService(IUserRepository repo) : IAdminProfileService
{
    public async Task<IReadOnlyList<UserProfile>> GetPendingSellersAsync(CancellationToken ct = default)
    {
        return await repo.Query()
            .Where(p => p.IsSeller && !p.IsSellerVerified)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UserProfile>> GetAdminsAsync(CancellationToken ct = default)
    {
        return await repo.Query()
            .Where(p => p.IsAdmin)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task ToggleAdminAsync(Guid userId, bool makeAdmin, CancellationToken ct = default)
    {
        var profile = await repo.Query().FirstOrDefaultAsync(p => p.UserId == userId, ct).ConfigureAwait(false)
                      ?? throw new InvalidOperationException("Profile not found");
        profile.IsAdmin = makeAdmin;
        await repo.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<UserProfile> MakeAdminByEmailAsync(string email, CancellationToken ct = default)
    {
        var profile = await repo.Query().FirstOrDefaultAsync(p => p.Email == email, ct).ConfigureAwait(false)
                      ?? throw new InvalidOperationException("User profile with this email was not found");
        profile.IsAdmin = true;
        await repo.SaveChangesAsync(ct).ConfigureAwait(false);
        return profile;
    }

    public async Task ToggleSellerVerificationAsync(Guid userId, bool verify, CancellationToken ct = default)
    {
        var profile = await repo.Query().FirstOrDefaultAsync(p => p.UserId == userId, ct).ConfigureAwait(false)
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

        await repo.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

