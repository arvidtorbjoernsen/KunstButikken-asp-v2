using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KunstButikken.ServiceDefaults;
using KunstButikken.UserService.Data;
using KunstButikken.UserService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController(IUserRepository repo, IDateTimeProvider clock) : ControllerBase
{
    private readonly IDateTimeProvider _clock = clock;

    [HttpGet("me")]
    public Task<ActionResult<UserProfile>> GetMe() => GetMeImpl();

    private async Task<ActionResult<UserProfile>> GetMeImpl()
    {
        if (!TryGetUserId(out var uid))
        {
            return Unauthorized();
        }

        LogRolesForDebug(uid);

        var (profile, created) = await GetOrCreateProfileAsync(uid).ConfigureAwait(false);
        if (created)
            // Already logged inside helper when created; return created profile
        {
            return Ok(profile);
        }

        await SyncRolesFromClaimsAsync(profile).ConfigureAwait(false);
        Console.WriteLine(
            $"[ProfileController] Returning profile with IsSeller: {profile.IsSeller}, IsAdmin: {profile.IsAdmin}");
        return Ok(profile);
    }

    // Extracted helper: get existing profile or create from claims and persist
    private async Task<(UserProfile profile, bool created)> GetOrCreateProfileAsync(Guid uid)
    {
        var profile = await repo.Query().FirstOrDefaultAsync(p => p.UserId == uid).ConfigureAwait(false);
        if (profile is not null)
        {
            return (profile, false);
        }

        profile = CreateProfileFromClaims(uid);
        Console.WriteLine(
            $"[ProfileController] Created new profile for user {uid}, IsSeller: {profile.IsSeller}, IsAdmin: {profile.IsAdmin}");
        await repo.AddAsync(profile).ConfigureAwait(false);
        await repo.SaveChangesAsync().ConfigureAwait(false);
        return (profile, true);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UserProfile input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (!TryGetUserId(out var uid))
        {
            return Unauthorized();
        }

        var profile = await repo.Query().FirstOrDefaultAsync(p => p.UserId == uid).ConfigureAwait(false);
        if (profile is null)
        {
            return NotFound();
        }

        ApplyProfileUpdates(profile, input);

        await repo.SaveChangesAsync().ConfigureAwait(false);
        return NoContent();
    }

    // Extracted helper: centralize updating of editable profile fields
    private void ApplyProfileUpdates(UserProfile profile, UserProfile input)
    {
        // Allow updating user-editable fields, but do not allow the user to set
        // IsSellerVerified or IsAdmin directly.
        profile.DisplayName = input.DisplayName;
        profile.Email = input.Email;
        profile.FullName = input.FullName;

        UpdateContactAndAddress(profile, input);
        UpdateSellerFlags(profile, input);

        profile.ProfileImageUrl = input.ProfileImageUrl;
        profile.PreferencesJson = input.PreferencesJson;
        profile.UpdatedAt = _clock.UtcNow;
    }

    private static void UpdateContactAndAddress(UserProfile profile, UserProfile input)
    {
        // Update contact information
        profile.PhoneNumber = input.PhoneNumber;

        // Update shipping address
        profile.Address = input.Address;
        profile.City = input.City;
        profile.PostalCode = input.PostalCode;
        profile.Country = input.Country;
    }

    private static void UpdateSellerFlags(UserProfile profile, UserProfile input)
    {
        // If user toggles seller status, reset verification flag when enabling.
        if (input.IsSeller && !profile.IsSeller)
        {
            profile.IsSeller = true;
            profile.IsSellerVerified = false;
        }
        else if (!input.IsSeller && profile.IsSeller)
        {
            // turning off seller removes verification
            profile.IsSeller = false;
            profile.IsSellerVerified = false;
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserProfile>> Register([FromBody] RegistrationRequest? req)
    {
        ArgumentNullException.ThrowIfNull(req);

        if (IsInvalidRegistration(req))
        {
            return BadRequest("Missing required fields");
        }

        // Require an authenticated user for register so the profile links to the identity
        if (!TryGetUserId(out var uid))
        {
            return Unauthorized();
        }

        var profile = await repo.Query().FirstOrDefaultAsync(p => p.UserId == uid).ConfigureAwait(false);
        if (profile is null)
        {
            profile = new UserProfile { Id = Guid.NewGuid(), UserId = uid, CreatedAt = _clock.UtcNow };
            await repo.AddAsync(profile).ConfigureAwait(false);
        }

        // Apply registration fields (guarded by validation above)
        // Copy into locals to clarify nullability for the analyzer
        var email = req!.Email ?? string.Empty;
        var fullName = req.FullName ?? string.Empty;
        var displayName = req.DisplayName ?? string.Empty;
        var userType = req.UserType;

        profile.Email = email;
        profile.FullName = fullName;
        profile.DisplayName = displayName;
        profile.IsSeller = string.Equals(userType, "seller", StringComparison.OrdinalIgnoreCase);
        if (profile.IsSeller)
        {
            profile.IsSellerVerified = false;
        }

        await repo.SaveChangesAsync().ConfigureAwait(false);
        return Ok(profile);
    }

    private static bool IsInvalidRegistration([NotNullWhen(false)] RegistrationRequest? req)
    {
        return req is null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.FullName) ||
               string.IsNullOrWhiteSpace(req.DisplayName);
    }

    private void LogRolesForDebug(Guid uid)
    {
        var roles = User.FindAll("roles").Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        var claimRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
        var allRoles = roles.Concat(claimRoles).Distinct().ToList();
        Console.WriteLine($"[ProfileController] User {uid} has roles: {string.Join(", ", allRoles)}");
        Console.WriteLine($"[ProfileController] IsInRole('seller'): {User.IsInRole("seller")}");
        Console.WriteLine($"[ProfileController] IsInRole('admin'): {User.IsInRole("admin")}");
    }

    private UserProfile CreateProfileFromClaims(Guid uid)
    {
        var nameClaim = GetClaimFirstValue(ClaimTypes.Name, "name") ?? User.Identity?.Name;
        var emailClaim = GetClaimFirstValue(ClaimTypes.Email, "email") ?? string.Empty;

        return new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = uid,
            DisplayName = !string.IsNullOrWhiteSpace(nameClaim) ? nameClaim : "Anonymous",
            Email = emailClaim,
            FullName = nameClaim ?? string.Empty,
            IsSeller = User.IsInRole("seller"),
            IsSellerVerified = false,
            IsAdmin = User.IsInRole("admin"),
            CreatedAt = _clock.UtcNow
        };
    }

    // Return the first non-empty claim value for the provided claim types
    private string? GetClaimFirstValue(params string[] claimTypes)
    {
        foreach (var t in claimTypes)
        {
            var v = User.FindFirst(t)?.Value;
            if (!string.IsNullOrWhiteSpace(v))
            {
                return v;
            }
        }

        return null;
    }

    // Create a deterministic Guid from an arbitrary string using SHA256 (truncate to 16 bytes)
    private static Guid DeterministicGuidFromString(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        var g = new byte[16];
        Array.Copy(hash, g, 16);
        // Set variant (RFC 4122)
        g[8] = (byte)((g[8] & 0x3F) | 0x80);
        // Set version to 5 (name-based). SHA-256 isn't v5 but using 5 makes the GUID version field meaningful.
        g[6] = (byte)((g[6] & 0x0F) | (5 << 4));
        return new Guid(g);
    }

    private bool TryGetUserId(out Guid uid)
    {
        uid = Guid.Empty;
        if (User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var sub = GetClaimFirstValue(ClaimTypes.NameIdentifier, "sub");
        if (string.IsNullOrWhiteSpace(sub))
        {
            return false;
        }

        if (Guid.TryParse(sub, out uid))
        {
            return true;
        }

        uid = DeterministicGuidFromString(sub);
        return true;
    }

    private async Task SyncRolesFromClaimsAsync(UserProfile profile)
    {
        var changedSeller = UpdateSellerStatusFromClaims(profile);
        var changedAdmin = UpdateAdminStatusFromClaims(profile);

        // Persist only if something changed
        if (!changedSeller && !changedAdmin)
        {
            return;
        }

        await repo.SaveChangesAsync().ConfigureAwait(false);
    }

    private bool UpdateSellerStatusFromClaims(UserProfile profile)
    {
        var hasSellerRole = User.IsInRole("seller");
        if (hasSellerRole && !profile.IsSeller)
        {
            Console.WriteLine($"[ProfileController] User {profile.UserId} gained Seller role, updating profile");
            profile.IsSeller = true;
            profile.IsSellerVerified = false; // Needs verification when becoming seller
            return true;
        }

        if (!hasSellerRole && profile.IsSeller)
        {
            Console.WriteLine($"[ProfileController] User {profile.UserId} lost Seller role, updating profile");
            profile.IsSeller = false;
            profile.IsSellerVerified = false; // Remove verification if no longer seller
            return true;
        }

        return false;
    }

    private bool UpdateAdminStatusFromClaims(UserProfile profile)
    {
        var hasAdminRole = User.IsInRole("admin");
        if (profile.IsAdmin != hasAdminRole)
        {
            Console.WriteLine($"[ProfileController] User {profile.UserId} admin status changed to: {hasAdminRole}");
            profile.IsAdmin = hasAdminRole;
            return true;
        }

        return false;
    }

    [HttpPost("{id:guid}/verify")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> VerifySeller(Guid id, [FromBody] VerifyRequest? req)
    {
        if (req is null)
        {
            return BadRequest("Missing payload");
        }

        var profile = await repo.Query().FirstOrDefaultAsync(p => p.Id == id).ConfigureAwait(false);
        if (profile is null)
        {
            return NotFound();
        }

        if (!profile.IsSeller)
        {
            return BadRequest("User is not a seller");
        }

        profile.IsSellerVerified = req.Verified;
        await repo.SaveChangesAsync().ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("unverified")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<List<UnverifiedSellerDto>>> ListUnverifiedSellers()
    {
        var list = await repo.Query()
            .Where(p => p.IsSeller && !p.IsSellerVerified)
            .Select(p => new UnverifiedSellerDto
            {
                Id = p.Id, UserId = p.UserId, DisplayName = p.DisplayName, Email = p.Email
            })
            .ToListAsync().ConfigureAwait(false);

        return Ok(list);
    }

    // DTOs moved to ProfileRequests.cs
}
