using KunstButikken.Common.Logging;

namespace KunstButikken.UserService.Services;

public class DbMigrationHostedService(IDbMigrationRunner runner, ILogger<DbMigrationHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogMessages.Information_Msg_6(logger, null);
        await runner.RunMigrationsAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        LogMessages.Information_Msg_7(logger, null);
        return Task.CompletedTask;
    }
}
