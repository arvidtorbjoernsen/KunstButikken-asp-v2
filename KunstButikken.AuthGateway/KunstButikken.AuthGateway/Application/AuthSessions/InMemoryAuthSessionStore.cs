using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using KunstButikken.AuthGateway.Domain;
using KunstButikken.AuthGateway.Domain.Interfaces;

namespace KunstButikken.AuthGateway.Application.AuthSessions;

public sealed class InMemoryAuthSessionStore : IAuthSessionStore
{
    private readonly ConcurrentDictionary<string, AuthSession> _sessions = new();
    private readonly ConcurrentDictionary<string, string> _refreshToSession = new();

    public bool HasActiveSessions => !_sessions.IsEmpty;

    public Task<AuthSession?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<AuthSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (_refreshToSession.TryGetValue(refreshToken, out var sessionId) && _sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult<AuthSession?>(session);
        }

        return Task.FromResult<AuthSession?>(null);
    }

    public Task UpsertAsync(AuthSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.SessionId] = session;
        _refreshToSession[session.RefreshToken] = session.SessionId;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryRemove(sessionId, out var removed))
        {
            _refreshToSession.TryRemove(removed.RefreshToken, out _);
        }
        return Task.CompletedTask;
    }

    public Task DeleteByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (_refreshToSession.TryRemove(refreshToken, out var sessionId))
        {
            _sessions.TryRemove(sessionId, out _);
        }

        return Task.CompletedTask;
    }
}
