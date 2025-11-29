using System;
using System.Collections.Generic;

namespace KunstButikken.AuthGateway.Application.AuthSessions;

public record AuthSession(
    string SessionId,
    string RefreshToken,
    string UserId,
    string? PreferredUsername,
    string? DisplayName,
    string? Email,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset RefreshExpiresAt);

