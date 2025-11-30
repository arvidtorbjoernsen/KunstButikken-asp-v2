using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace KunstButikken.AuthGateway.Application.AuthSessions;

public sealed class AuthSessionService(IAuthSessionStore store) : IAuthSessionService
{
    private readonly IAuthSessionStore _store = store;

    public async Task<AuthSession> CreateSessionFromTokenAsync(string accessToken, string refreshToken, TimeSpan sessionLifetime, TimeSpan refreshLifetime, CancellationToken cancellationToken = default)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        var userId = jwt.Subject ?? jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Guid.NewGuid().ToString("N");
        var preferredUsername = jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
        var displayName = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
        var email = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value;
        var roles = jwt.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var now = DateTimeOffset.UtcNow;
        var session = new AuthSession(
            SessionId: Guid.NewGuid().ToString("N"),
            RefreshToken: refreshToken,
            UserId: userId,
            PreferredUsername: preferredUsername,
            DisplayName: displayName,
            Email: email,
            Roles: roles,
            CreatedAt: now,
            ExpiresAt: now.Add(sessionLifetime),
            RefreshExpiresAt: now.Add(refreshLifetime));

        await _store.UpsertAsync(session, cancellationToken).ConfigureAwait(false);
        return session;
    }

    public Task<AuthSession> CreateSessionFromKeycloakAsync(KeycloakTokenResponse tokenResponse, TimeSpan sessionLifetime, TimeSpan refreshLifetime, CancellationToken cancellationToken = default)
        => CreateSessionFromTokenAsync(tokenResponse.access_token, tokenResponse.refresh_token, sessionLifetime, refreshLifetime, cancellationToken);

    public async Task<AuthSession?> ValidateSessionAsync(string sessionId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var session = await _store.GetAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return null;
        }

        if (session.ExpiresAt <= now)
        {
            await _store.DeleteAsync(session.SessionId, cancellationToken).ConfigureAwait(false);
            return null;
        }

        return session;
    }

    public async Task<AuthSession?> RotateRefreshTokenAsync(string refreshToken, string newRefreshToken, DateTimeOffset now, TimeSpan refreshLifetime, CancellationToken cancellationToken = default)
    {
        var session = await _store.GetByRefreshTokenAsync(refreshToken, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return null;
        }

        if (session.RefreshExpiresAt <= now)
        {
            await _store.DeleteByRefreshTokenAsync(refreshToken, cancellationToken).ConfigureAwait(false);
            return null;
        }

        var updated = session with
        {
            RefreshToken = newRefreshToken,
            RefreshExpiresAt = now.Add(refreshLifetime)
        };

        await _store.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
        return updated;
    }

    public Task RemoveSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        => _store.DeleteAsync(sessionId, cancellationToken);

    public Task RemoveByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        => _store.DeleteByRefreshTokenAsync(refreshToken, cancellationToken);
}
