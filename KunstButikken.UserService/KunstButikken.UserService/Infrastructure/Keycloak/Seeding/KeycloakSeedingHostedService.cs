using KunstButikken.Common.Logging;

namespace KunstButikken.UserService.Infrastructure.Keycloak.Seeding;

public sealed class KeycloakSeedingHostedService(
    IKeycloakSeeder keycloakSeeder,
    ILogger<KeycloakSeedingHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogMessages.Information_Msg_8(logger, null);

        await keycloakSeeder.ApplyMigrationsAsync(stoppingToken).ConfigureAwait(false);

        const int maxRetries = 10;
        const int retryDelaySeconds = 5;

        for (var attempt = 1; attempt <= maxRetries && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                await keycloakSeeder.SeedKeycloakUsersAsync(stoppingToken).ConfigureAwait(false);
                LogMessages.Information_Attempt_9(logger, attempt, null);
                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                LogMessages.Warning_Attempt_Delay_10(logger, attempt, retryDelaySeconds, ex);
                if (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), stoppingToken).ConfigureAwait(false);
                }
            }
        }

        if (!stoppingToken.IsCancellationRequested)
        {
            LogMessages.Error_MaxRetries_11(logger, maxRetries, null);
        }
    }
}
