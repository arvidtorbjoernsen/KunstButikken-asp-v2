using KunstButikken.Common.Logging;

namespace KunstButikken.AuctionService.Services;

public class AuctionSeedingHostedService(ILogger<AuctionSeedingHostedService> logger, IAuctionSeeder seeder)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogMessages.Information_Msg_19(logger, null);
        await seeder.ApplyMigrationsAsync(cancellationToken).ConfigureAwait(false);
        await seeder.SeedAuctionsIfEmptyAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        LogMessages.Information_Msg_20(logger, null);
        return Task.CompletedTask;
    }
}
