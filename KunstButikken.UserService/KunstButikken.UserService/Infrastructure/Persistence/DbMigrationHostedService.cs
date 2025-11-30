using System;
using System.Threading;
using System.Threading.Tasks;
using KunstButikken.UserService.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KunstButikken.UserService.Infrastructure.Persistence;

public sealed class DbMigrationHostedService(IServiceProvider serviceProvider, ILogger<DbMigrationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IDbMigrationRunner>();

        try
        {
            await runner.RunMigrationsAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed on startup.");
        }
    }
}
