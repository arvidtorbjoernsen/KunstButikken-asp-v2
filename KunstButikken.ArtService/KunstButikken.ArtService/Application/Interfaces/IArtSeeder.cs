namespace KunstButikken.ArtService.Application.Interfaces;

public interface IArtSeeder
{
    Task EnsureDatabaseMigratedAsync(CancellationToken cancellationToken);
    Task SeedArtAsync(CancellationToken cancellationToken = default);
}

