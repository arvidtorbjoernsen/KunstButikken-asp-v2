using System.Security.Claims;
using KunstButikken.ServiceDefaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunstButikken.AuthGateway.Controllers;

[ApiController]
[Route("[controller]")] // base path: /auth
public class AuthController(IDateTimeProvider clock) : ControllerBase
{
    private readonly IDateTimeProvider _clock = clock;

    [HttpGet("alive")]
    [AllowAnonymous]
    public IActionResult Alive() =>
        Ok(new
        {
            status = "ok", service = "AuthGateway", time = _clock.UtcNow
        });

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var user = HttpContext.User;

        // Log authentication details for debugging
        var hasAuth = user.Identity?.IsAuthenticated ?? false;
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var hasBearer = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

        Console.WriteLine($"[AuthController.Me] IsAuthenticated: {hasAuth}, HasBearerToken: {hasBearer}");

        if (!hasAuth)
        {
            Console.WriteLine(
                $"[AuthController.Me] User is not authenticated. Identity: {user.Identity?.GetType().Name ?? "null"}");
            return Unauthorized(new
            {
                error = "User is not authenticated"
            });
        }

        var name = user.FindFirst(ClaimTypes.Name)?.Value
                   ?? user.FindFirst("name")?.Value
                   ?? user.Identity!.Name;
        var preferredUsername = user.FindFirst("preferred_username")?.Value;
        var email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value;
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;

        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (roles.Count == 0)
        {
            roles = user.FindAll("roles").Select(c => c.Value).ToList();
        }

        var claims = user.Claims.Select(c => new
        {
            type = c.Type, value = c.Value
        }).ToList();

        Console.WriteLine($"[AuthController.Me] Success - User: {name}, Sub: {sub}, Roles: {string.Join(", ", roles)}");

        return Ok(new
        {
            sub,
            name,
            preferredUsername,
            email,
            roles,
            claims
        });
    }
}
