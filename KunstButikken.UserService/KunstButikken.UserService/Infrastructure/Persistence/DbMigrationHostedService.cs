using KunstButikken.Common.Logging;
using KunstButikken.UserService.Application.Interfaces;

namespace KunstButikken.UserService.Infrastructure.Persistence;

public sealed class DbMigrationHostedService(IDbMigrationRunner runner, ILogger<DbMigrationHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogMessages.Information_Msg_6(logger, null);
        await runner.RunMigrationsAsync(stoppingToken).ConfigureAwait(false);
    }
}
