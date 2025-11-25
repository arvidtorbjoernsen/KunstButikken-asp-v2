using KunstButikken.IntegrationEvents.Contracts.Abstractions;

// Updated namespace

namespace KunstButikken.ArtService.Data;

public class ArtRepository : Infrastructure.Repositories.ArtRepository, Domain.Interfaces.IArtRepository
{
    public ArtRepository(ArtDbContext db, IEventBus eventBus)
        : base(db, eventBus)
    {
    }
}
