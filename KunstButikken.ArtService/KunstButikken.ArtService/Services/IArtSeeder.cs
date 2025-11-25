namespace KunstButikken.ArtService.Services;

public interface IArtSeeder
{
  Task EnsureDatabaseMigratedAsync(CancellationToken cancellationToken);
  Task SeedArtAsync(CancellationToken cancellationToken = default);
}