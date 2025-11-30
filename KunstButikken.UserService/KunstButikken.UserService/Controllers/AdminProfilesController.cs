using KunstButikken.UserService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunstButikken.UserService.Controllers;

[ApiController]
[Route("api/admin/profiles")]
[Authorize(Roles = "Admin")]
public class AdminProfilesController(IAdminProfileService adminProfiles) : ControllerBase
{
    [HttpGet("pending-sellers")]
    public async Task<IActionResult> GetPendingSellers()
    {
        var list = await adminProfiles.GetPendingSellersAsync().ConfigureAwait(false);
        return Ok(list);
    }

    [HttpGet("admins")]
    public async Task<IActionResult> GetAdmins()
    {
        var list = await adminProfiles.GetAdminsAsync().ConfigureAwait(false);
        return Ok(list);
    }

    [HttpPost("{userId:guid}/make-admin")]
    [HttpPost("{userId:guid}/remove-admin")]
    public async Task<IActionResult> ToggleAdmin(Guid userId)
    {
        var make = HttpContext.Request.Path.Value?.EndsWith("/make-admin", StringComparison.OrdinalIgnoreCase) ?? false;
        try
        {
            await adminProfiles.ToggleAdminAsync(userId, make).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
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

        try
        {
            var profile = await adminProfiles.MakeAdminByEmailAsync(req.Email.Trim()).ConfigureAwait(false);
            return Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{userId:guid}/verify-seller")]
    [HttpPost("{userId:guid}/reject-seller")]
    public async Task<IActionResult> ToggleSellerVerification(Guid userId)
    {
        var isVerify = HttpContext.Request.Path.Value?.EndsWith("/verify-seller", StringComparison.OrdinalIgnoreCase) ??
                       false;
        try
        {
            await adminProfiles.ToggleSellerVerificationAsync(userId, isVerify).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return NotFound();
        }
        return NoContent();
    }
}
