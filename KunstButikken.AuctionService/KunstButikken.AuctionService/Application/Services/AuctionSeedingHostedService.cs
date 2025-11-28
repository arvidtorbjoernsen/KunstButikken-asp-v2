using KunstButikken.AuctionService.Domain.Interfaces;
using KunstButikken.Common.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KunstButikken.AuctionService.Application.Services;

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
