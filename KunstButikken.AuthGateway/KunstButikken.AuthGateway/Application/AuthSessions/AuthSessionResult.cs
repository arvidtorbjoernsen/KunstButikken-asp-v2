namespace KunstButikken.AuthGateway.Application.AuthSessions;

public sealed record AuthSessionResult(
    string SessionId,
    string UserId,
    string? PreferredUsername,
    string? DisplayName,
    string? Email,
    IReadOnlyList<string> Roles,
    DateTimeOffset ExpiresAt);

