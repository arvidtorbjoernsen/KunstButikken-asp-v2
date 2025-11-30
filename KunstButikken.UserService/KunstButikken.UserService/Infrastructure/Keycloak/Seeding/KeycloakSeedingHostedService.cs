using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KunstButikken.UserService.Infrastructure.Keycloak.Seeding;

public sealed class KeycloakSeedingHostedService(IServiceProvider serviceProvider, ILogger<KeycloakSeedingHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IKeycloakSeeder>();

        try
        {
            await seeder.SeedKeycloakUsersAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Keycloak user seeding failed on startup.");
        }
    }
}
