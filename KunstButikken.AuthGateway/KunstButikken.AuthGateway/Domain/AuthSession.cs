namespace KunstButikken.AuthGateway.Domain;

public sealed record AuthSession(
    string SessionId,
    string RefreshToken,
    string UserId,
    string? PreferredUsername,
    string? DisplayName,
    string? Email,
    string[] Roles,
    string AccessToken,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset RefreshExpiresAt
);
