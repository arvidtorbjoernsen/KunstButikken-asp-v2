using KunstButikken.AuthGateway.Domain;

namespace KunstButikken.AuthGateway.Application.AuthSessions;

public interface IAuthSessionService
{
    Task<AuthSession> CreateSessionFromTokenAsync(string accessToken, string refreshToken, TimeSpan sessionLifetime, TimeSpan refreshLifetime, CancellationToken cancellationToken = default);
    Task<AuthSession> CreateSessionFromKeycloakAsync(KeycloakTokenResponse tokenResponse, TimeSpan sessionLifetime, TimeSpan refreshLifetime, CancellationToken cancellationToken = default);
    Task<AuthSession?> ValidateSessionAsync(string sessionId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<AuthSession?> RotateRefreshTokenAsync(string refreshToken, string newRefreshToken, DateTimeOffset now, TimeSpan refreshLifetime, CancellationToken cancellationToken = default);
    Task RemoveSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task RemoveByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
