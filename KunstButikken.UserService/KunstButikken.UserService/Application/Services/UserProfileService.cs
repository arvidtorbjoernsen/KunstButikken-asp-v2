namespace KunstButikken.UserService.Application.Services;

using System.Security.Claims;
using KunstButikken.ServiceDefaults;
using KunstButikken.UserService.Application.Interfaces;
using KunstButikken.UserService.Domain.Dtos;
using KunstButikken.UserService.Domain.Interfaces;
using KunstButikken.UserService.Models;
using Microsoft.EntityFrameworkCore;

public sealed class UserProfileService : IUserProfileService
{
    private readonly IUserRepository _repo;
    private readonly IDateTimeProvider _clock;

    public UserProfileService(IUserRepository repo, IDateTimeProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public async Task<UserProfile> GetOrCreateProfileAsync(Guid userId, ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var profile = await _repo.Query().FirstOrDefaultAsync(p => p.UserId == userId, ct).ConfigureAwait(false);
        if (profile is not null)
        {
            await SyncRolesFromClaimsAsync(profile, principal, ct).ConfigureAwait(false);
            return profile;
        }

        profile = CreateProfileFromClaims(userId, principal);
        await _repo.AddAsync(profile, ct).ConfigureAwait(false);
        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
        return profile;
    }

    public async Task<UserProfile> UpdateProfileAsync(Guid userId, UserProfile input, CancellationToken ct = default)
    {
        var profile = await _repo.Query().FirstOrDefaultAsync(p => p.UserId == userId, ct).ConfigureAwait(false)
                      ?? throw new InvalidOperationException("Profile not found");

        profile.DisplayName = input.DisplayName;
        profile.Email = input.Email;
        profile.FullName = input.FullName;
        profile.PhoneNumber = input.PhoneNumber;
        profile.Address = input.Address;
        profile.City = input.City;
        profile.PostalCode = input.PostalCode;
        profile.Country = input.Country;
        profile.ProfileImageUrl = input.ProfileImageUrl;
        profile.PreferencesJson = input.PreferencesJson;
        profile.UpdatedAt = _clock.UtcNow;

        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
        return profile;
    }

    public async Task<UserProfile> RegisterAsync(Guid userId, RegistrationRequest request, CancellationToken ct = default)
    {
        var profile = await _repo.Query().FirstOrDefaultAsync(p => p.UserId == userId, ct).ConfigureAwait(false);
        if (profile is null)
        {
            profile = new UserProfile { Id = Guid.NewGuid(), UserId = userId, CreatedAt = _clock.UtcNow };
            await _repo.AddAsync(profile, ct).ConfigureAwait(false);
        }

        profile.Email = request.Email ?? string.Empty;
        profile.FullName = request.FullName ?? string.Empty;
        profile.DisplayName = request.DisplayName ?? string.Empty;
        profile.IsSeller = string.Equals(request.UserType, "seller", StringComparison.OrdinalIgnoreCase);
        if (profile.IsSeller)
        {
            profile.IsSellerVerified = false;
        }

        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
        return profile;
    }

    public async Task VerifySellerAsync(Guid profileId, bool verified, CancellationToken ct = default)
    {
        var profile = await _repo.Query().FirstOrDefaultAsync(p => p.Id == profileId, ct).ConfigureAwait(false)
                      ?? throw new InvalidOperationException("Profile not found");
        if (!profile.IsSeller)
        {
            throw new InvalidOperationException("User is not a seller");
        }

        profile.IsSellerVerified = verified;
        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UnverifiedSellerDto>> ListUnverifiedSellersAsync(CancellationToken ct = default)
    {
        return await _repo.Query()
            .Where(p => p.IsSeller && !p.IsSellerVerified)
            .Select(p => new UnverifiedSellerDto
            {
                Id = p.Id,
                UserId = p.UserId,
                DisplayName = p.DisplayName,
                Email = p.Email
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static UserProfile CreateProfileFromClaims(Guid userId, ClaimsPrincipal principal)
    {
        var displayName = principal.FindFirst(ClaimTypes.Name)?.Value ?? principal.Identity?.Name ?? "Anonymous";
        var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

        return new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = displayName,
            Email = email,
            FullName = displayName,
            IsSeller = principal.IsInRole("seller"),
            IsSellerVerified = false,
            IsAdmin = principal.IsInRole("admin"),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task SyncRolesFromClaimsAsync(UserProfile profile, ClaimsPrincipal principal, CancellationToken ct)
    {
        var changed = false;
        var hasSellerRole = principal.IsInRole("seller");
        if (profile.IsSeller != hasSellerRole)
        {
            profile.IsSeller = hasSellerRole;
            profile.IsSellerVerified = false;
            changed = true;
        }

        var hasAdminRole = principal.IsInRole("admin");
        if (profile.IsAdmin != hasAdminRole)
        {
            profile.IsAdmin = hasAdminRole;
            changed = true;
        }

        if (changed)
        {
            await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
