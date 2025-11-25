using KunstButikken.Common.Logging;

namespace KunstButikken.ArtService.Services;

public class ArtSeedingHostedService(ILogger<ArtSeedingHostedService> logger, IArtSeeder seeder) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogMessages.Information_Msg_32(logger, null);
        await seeder.SeedArtAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        LogMessages.Information_Msg_33(logger, null);
        return Task.CompletedTask;
    }
}
