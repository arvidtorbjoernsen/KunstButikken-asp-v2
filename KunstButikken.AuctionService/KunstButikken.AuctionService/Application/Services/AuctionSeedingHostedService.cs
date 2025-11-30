using KunstButikken.AuctionService.Domain.Interfaces;
using KunstButikken.Common.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KunstButikken.AuctionService.Application.Services;

public class AuctionSeedingHostedService(
    ILogger<AuctionSeedingHostedService> logger,
    IServiceProvider serviceProvider)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogMessages.Information_Msg_19(logger, null);
        using var scope = serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IAuctionSeeder>();
        await seeder.ApplyMigrationsAsync(cancellationToken).ConfigureAwait(false);
        await seeder.SeedAuctionsIfEmptyAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        LogMessages.Information_Msg_20(logger, null);
        return Task.CompletedTask;
    }
}
