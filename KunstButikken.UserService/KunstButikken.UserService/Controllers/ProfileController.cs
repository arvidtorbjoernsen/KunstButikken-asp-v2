using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KunstButikken.ServiceDefaults;
using KunstButikken.UserService.Application.Interfaces;
using KunstButikken.UserService.Domain.Dtos;
using KunstButikken.UserService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunstButikken.UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController(IUserProfileService profiles) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserProfile>> GetMe()
    {
        if (!TryGetUserId(out var uid))
        {
            return Unauthorized();
        }

        var profile = await profiles.GetOrCreateProfileAsync(uid, User).ConfigureAwait(false);
        return Ok(profile);
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

        await profiles.UpdateProfileAsync(uid, input).ConfigureAwait(false);
        return NoContent();
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

        var profile = await profiles.RegisterAsync(uid, req!).ConfigureAwait(false);
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
            CreatedAt = DateTime.UtcNow
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

    [HttpPost("{id:guid}/verify")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> VerifySeller(Guid id, [FromBody] VerifyRequest? req)
    {
        if (req is null)
        {
            return BadRequest("Missing payload");
        }

        try
        {
            await profiles.VerifySellerAsync(id, req.Verified).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        return NoContent();
    }

    [HttpGet("unverified")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<List<UnverifiedSellerDto>>> ListUnverifiedSellers()
    {
        var list = await profiles.ListUnverifiedSellersAsync().ConfigureAwait(false);
        return Ok(list);
    }
}
