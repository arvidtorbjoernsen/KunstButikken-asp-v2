using System.Threading;
using System.Threading.Tasks;

namespace KunstButikken.AuthGateway.Application.AuthSessions;

public interface IAuthSessionStore
{
    Task<AuthSession?> GetAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<AuthSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task UpsertAsync(AuthSession session, CancellationToken cancellationToken = default);
    Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default);
    Task DeleteByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    bool HasActiveSessions { get; }
}
