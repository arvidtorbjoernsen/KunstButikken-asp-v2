using System.Threading;
using System.Threading.Tasks;
using KunstButikken.AuthGateway.Domain;

namespace KunstButikken.AuthGateway.Domain.Interfaces
{
    public interface IAuthSessionStore
    {
        Task UpsertAsync(AuthSession session, CancellationToken cancellationToken = default);
        Task<AuthSession?> GetAsync(string sessionId, CancellationToken cancellationToken = default);
        Task<AuthSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default);
        Task DeleteByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    }
}
