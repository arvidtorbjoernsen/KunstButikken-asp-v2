namespace KunstButikken.AuctionService.Services;

public interface IAuctionSeeder
{
  Task ApplyMigrationsAsync(CancellationToken ct = default);
  Task SeedAuctionsIfEmptyAsync(CancellationToken ct = default);
}