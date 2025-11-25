using KunstButikken.UserService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.UserService.Controllers;

[ApiController]
[Route("api/admin/profiles")]
[Authorize(Roles = "Admin")]
public class AdminProfilesController(IUserRepository repo) : ControllerBase
{
    [HttpGet("pending-sellers")]
    public async Task<IActionResult> GetPendingSellers()
    {
        var list = await repo.Query()
            .Where(p => p.IsSeller && !p.IsSellerVerified)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync().ConfigureAwait(false);
        return Ok(list);
    }

    [HttpGet("admins")]
    public async Task<IActionResult> GetAdmins()
    {
        var list = await repo.Query()
            .Where(p => p.IsAdmin)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync().ConfigureAwait(false);
        return Ok(list);
    }

    [HttpPost("{userId:guid}/make-admin")]
    [HttpPost("{userId:guid}/remove-admin")]
    public async Task<IActionResult> ToggleAdmin(Guid userId)
    {
        var make = HttpContext.Request.Path.Value?.EndsWith("/make-admin", StringComparison.OrdinalIgnoreCase) ?? false;
        var profile = await repo.Query().FirstOrDefaultAsync(p => p.UserId == userId).ConfigureAwait(false);
        if (profile is null)
        {
            return NotFound();
        }

        profile.IsAdmin = make;
        await repo.SaveChangesAsync().ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("make-admin-by-email")]
    public async Task<IActionResult> MakeAdminByEmail([FromBody] MakeAdminByEmailRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);

        if (string.IsNullOrWhiteSpace(req.Email))
        {
            return BadRequest("Email required");
        }

        var profile = await repo.Query().FirstOrDefaultAsync(p => p.Email == req.Email).ConfigureAwait(false);
        if (profile is null)
        {
            return NotFound("User profile with this email was not found");
        }

        profile.IsAdmin = true;
        await repo.SaveChangesAsync().ConfigureAwait(false);
        return Ok(profile);
    }

    [HttpPost("{userId:guid}/verify-seller")]
    [HttpPost("{userId:guid}/reject-seller")]
    public async Task<IActionResult> ToggleSellerVerification(Guid userId)
    {
        var isVerify = HttpContext.Request.Path.Value?.EndsWith("/verify-seller", StringComparison.OrdinalIgnoreCase) ??
                       false;
        var profile = await repo.Query().FirstOrDefaultAsync(p => p.UserId == userId).ConfigureAwait(false);
        if (profile is null)
        {
            return NotFound();
        }

        if (isVerify)
        {
            if (!profile.IsSeller)
            {
                return BadRequest("User is not a seller");
            }

            profile.IsSellerVerified = true;
        }
        else
        {
            profile.IsSellerVerified = false;
            profile.IsSeller = false;
        }

        await repo.SaveChangesAsync().ConfigureAwait(false);
        return NoContent();
    }
}
