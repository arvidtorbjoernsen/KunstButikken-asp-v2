using KunstButikken.Common.Logging;

namespace KunstButikken.UserService.Services;

public class KeycloakSeedingHostedService(ILogger<KeycloakSeedingHostedService> logger, IKeycloakSeeder seeder)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogMessages.Information_Msg_8(logger, null);

        await seeder.ApplyMigrationsAsync(cancellationToken).ConfigureAwait(false);

        const int maxRetries = 10;
        const int retryDelaySeconds = 5;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await seeder.SeedKeycloakUsersAsync(cancellationToken).ConfigureAwait(false);
                LogMessages.Information_Attempt_9(logger, attempt, null);
                return;
            }
            catch (Exception ex)
            {
                LogMessages.Warning_Attempt_Delay_10(logger, attempt, retryDelaySeconds, ex);
                if (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        LogMessages.Error_MaxRetries_11(logger, maxRetries, null);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        LogMessages.Information_Msg_12(logger, null);
        return Task.CompletedTask;
    }
}
