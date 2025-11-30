using System.Security.Claims;
using KunstButikken.AuthGateway.Application.AuthSessions;
using KunstButikken.ServiceDefaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KunstButikken.AuthGateway.Controllers;

[ApiController]
[Route("[controller]")] // base path: /auth
public class AuthController(
    IDateTimeProvider clock,
    IAuthSessionService sessionService,
    IKeycloakTokenClient keycloakClient,
    IAuthCookieWriter cookieWriter,
    IOptions<AuthSessionOptions> options
) : ControllerBase
{
    private readonly IDateTimeProvider _clock = clock;
    private readonly IAuthSessionService _sessionService = sessionService;
    private readonly IKeycloakTokenClient _keycloakClient = keycloakClient;
    private readonly IAuthCookieWriter _cookieWriter = cookieWriter;
    private readonly AuthSessionOptions _options = options.Value;

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

    [HttpPost("session")]
    [AllowAnonymous]
    public async Task<IActionResult> IssueSession([FromBody] SessionRequest request, CancellationToken cancel)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.RedirectUri))
        {
            return BadRequest(new { error = "code and redirectUri are required" });
        }

        var tokens = await _keycloakClient.ExchangeCodeAsync(request.Code, request.RedirectUri, cancel).ConfigureAwait(false);
        if (tokens is null)
        {
            return Unauthorized(new { error = "Token exchange failed" });
        }

        var session = await _sessionService.CreateSessionFromKeycloakAsync(tokens, _options.SessionLifetime, _options.RefreshLifetime, cancel).ConfigureAwait(false);
        _cookieWriter.WriteSessionCookie(Response, session);
        _cookieWriter.WriteRefreshCookie(Response, session);
        _cookieWriter.WriteAccessTokenCookie(Response, session);

        return Ok(new { session.SessionId, session.UserId, session.Roles, session.ExpiresAt });
    }

    [HttpGet("session/validate")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateSession(CancellationToken cancel)
    {
        var sessionId = Request.Cookies[_options.SessionCookieName];
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized();
        }

        var session = await _sessionService.ValidateSessionAsync(sessionId, _clock.UtcNow, cancel).ConfigureAwait(false);
        if (session is null)
        {
            _cookieWriter.ClearCookies(Response);
            return Unauthorized();
        }

        return Ok(new
        {
            session.UserId,
            session.PreferredUsername,
            session.DisplayName,
            session.Email,
            session.Roles,
            session.ExpiresAt
        });
    }

    [HttpPost("session/refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshSession(CancellationToken cancel)
    {
        var refresh = Request.Cookies[_options.RefreshCookieName];
        if (string.IsNullOrEmpty(refresh))
        {
            return Unauthorized();
        }

        var tokens = await _keycloakClient.RefreshAsync(refresh, cancel).ConfigureAwait(false);
        if (tokens is null)
        {
            _cookieWriter.ClearCookies(Response);
            return Unauthorized();
        }

        var session = await _sessionService.CreateSessionFromKeycloakAsync(tokens, _options.SessionLifetime, _options.RefreshLifetime, cancel).ConfigureAwait(false);
        _cookieWriter.WriteRefreshCookie(Response, session);
        _cookieWriter.WriteSessionCookie(Response, session);
        _cookieWriter.WriteAccessTokenCookie(Response, session);

        return Ok(new { session.SessionId, session.ExpiresAt });
    }

    [HttpPost("session/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancel)
    {
        var sessionId = Request.Cookies[_options.SessionCookieName];
        var refresh = Request.Cookies[_options.RefreshCookieName];

        if (!string.IsNullOrEmpty(sessionId))
        {
            await _sessionService.RemoveSessionAsync(sessionId, cancel).ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(refresh))
        {
            await _sessionService.RemoveByRefreshTokenAsync(refresh, cancel).ConfigureAwait(false);
        }

        _cookieWriter.ClearCookies(Response);
        return Ok(new { status = "signed-out" });
    }
}

public sealed record SessionRequest(string Code, string RedirectUri);
