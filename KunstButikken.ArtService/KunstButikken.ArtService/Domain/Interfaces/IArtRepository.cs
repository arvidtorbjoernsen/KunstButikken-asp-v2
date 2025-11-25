using System.Linq;

namespace KunstButikken.ArtService.Domain.Interfaces;

public interface IArtRepository
{
    IQueryable<Models.Art> Query();
    Task<Models.Art?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Models.Art art, CancellationToken cancellationToken = default);
    Task RemoveAsync(Models.Art art, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

