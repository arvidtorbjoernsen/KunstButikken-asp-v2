using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using KunstButikken.AuthGateway.Application.AuthSessions;
using Xunit;

namespace KunstButikken.AuthGateway.Tests.AuthSessions;

public class AuthSessionServiceTests
{
    private static string CreateJwt(params Claim[] claims)
    {
        var token = new JwtSecurityToken(claims: claims);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task CreateSessionFromTokenAsync_StoresSession()
    {
        var store = new InMemoryAuthSessionStore();
        var service = new AuthSessionService(store);
        var jwt = CreateJwt(
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim("preferred_username", "jane"),
            new Claim(ClaimTypes.Email, "jane@example.com"),
            new Claim(ClaimTypes.Role, "admin"));

        var session = await service.CreateSessionFromTokenAsync(jwt, "refresh-token", TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));
        var stored = await store.GetAsync(session.SessionId);

        Assert.NotNull(stored);
        Assert.Equal("user-123", stored!.UserId);
        Assert.Contains("admin", stored.Roles);
    }

    [Fact]
    public async Task ValidateSessionAsync_ReturnsNullAfterExpiry()
    {
        var store = new InMemoryAuthSessionStore();
        var service = new AuthSessionService(store);
        var jwt = CreateJwt(new Claim(ClaimTypes.NameIdentifier, "user-1"));
        var session = await service.CreateSessionFromTokenAsync(jwt, "refresh", TimeSpan.FromMilliseconds(10), TimeSpan.FromMinutes(1));

        var result = await service.ValidateSessionAsync(session.SessionId, session.ExpiresAt.AddSeconds(1));

        Assert.Null(result);
        Assert.False(store.HasActiveSessions);
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_ExtendsLifetime()
    {
        var store = new InMemoryAuthSessionStore();
        var service = new AuthSessionService(store);
        var jwt = CreateJwt(new Claim(ClaimTypes.NameIdentifier, "user-rotate"));
        var session = await service.CreateSessionFromTokenAsync(jwt, "initial", TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        var rotated = await service.RotateRefreshTokenAsync("initial", "new-refresh", session.CreatedAt.AddMinutes(1), TimeSpan.FromMinutes(10));

        Assert.NotNull(rotated);
        Assert.Equal("new-refresh", rotated!.RefreshToken);
        Assert.True(rotated.RefreshExpiresAt > session.RefreshExpiresAt);
    }

    [Fact]
    public async Task CreateSessionFromKeycloakAsync_ParsesResponse()
    {
        var store = new InMemoryAuthSessionStore();
        var service = new AuthSessionService(store);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.WriteToken(new JwtSecurityToken(claims: new[] { new Claim(ClaimTypes.NameIdentifier, "kc-user") }));
        var response = new KeycloakTokenResponse(token, "refresh-xyz", 3600, 7200);

        var session = await service.CreateSessionFromKeycloakAsync(response, TimeSpan.FromMinutes(10), TimeSpan.FromHours(2));

        Assert.Equal("kc-user", session.UserId);
    }
}
