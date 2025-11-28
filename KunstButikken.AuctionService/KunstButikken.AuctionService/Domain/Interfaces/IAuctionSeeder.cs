namespace KunstButikken.AuctionService.Domain.Interfaces;

public interface IAuctionSeeder
{
    Task ApplyMigrationsAsync(CancellationToken ct = default);
    Task SeedAuctionsIfEmptyAsync(CancellationToken ct = default);
}

