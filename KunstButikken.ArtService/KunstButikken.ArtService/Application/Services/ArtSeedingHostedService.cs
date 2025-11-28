using KunstButikken.ArtService.Application.Interfaces;
using KunstButikken.Common.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KunstButikken.ArtService.Application.Services;

public class ArtSeedingHostedService : IHostedService
{
    private readonly ILogger<ArtSeedingHostedService> _logger;
    private readonly IArtSeeder _seeder;

    public ArtSeedingHostedService(ILogger<ArtSeedingHostedService> logger, IArtSeeder seeder)
    {
        _logger = logger;
        _seeder = seeder;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogMessages.Information_Msg_32(_logger, null);
        await _seeder.SeedArtAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        LogMessages.Information_Msg_33(_logger, null);
        return Task.CompletedTask;
    }
}

