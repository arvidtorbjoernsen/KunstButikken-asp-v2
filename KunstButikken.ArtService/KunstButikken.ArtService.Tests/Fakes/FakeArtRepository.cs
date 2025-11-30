using System.Linq.Expressions;
using KunstButikken.ArtService.Domain.Interfaces;
using KunstButikken.ArtService.Domain.Models;

namespace KunstButikken.ArtService.Tests.Fakes;

internal sealed class FakeArtRepository : IArtRepository
{
    private readonly List<Art> _items = new();

    public IQueryable<Art> Query() => _items.AsQueryable();

    public Task<Art?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.SingleOrDefault(a => a.Id == id));
    }

    public Task AddAsync(Art entity, CancellationToken cancellationToken = default)
    {
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Art entity, CancellationToken cancellationToken = default)
    {
        _items.Remove(entity);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public void Seed(params Art[] arts) => _items.AddRange(arts);
}
