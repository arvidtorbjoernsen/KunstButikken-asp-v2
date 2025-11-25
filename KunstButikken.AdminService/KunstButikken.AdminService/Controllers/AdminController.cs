using KunstButikken.AdminService.Application.Interfaces;
using KunstButikken.AdminService.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunstButikken.AdminService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")] // All actions require Admin member unless explicitly allowed
public class AdminController : ControllerBase
{
    private readonly IAdminService _svc;

    public AdminController(IAdminService svc)
    {
        _svc = svc;
    }

    [HttpPost("art/{id:guid}/approve")]
    public async Task<IActionResult> ApproveArt(Guid id)
    {
        var result = await _svc.ApproveArtAsync(id, User?.Identity?.Name ?? "system");
        return Accepted(result);
    }

    [HttpPost("art/{id:guid}/reject")]
    public async Task<IActionResult> RejectArt(Guid id, [FromBody] string reason)
    {
        var result = await _svc.RejectArtAsync(id, reason, User?.Identity?.Name ?? "system");
        return Accepted(result);
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() =>
        Ok(new HealthResponse
        {
            Status = "ok"
        });
}
