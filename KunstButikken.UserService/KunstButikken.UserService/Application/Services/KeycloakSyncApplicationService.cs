namespace KunstButikken.UserService.Application.Services;

using KunstButikken.UserService.Application.Interfaces;
using KunstButikken.UserService.Shared.Dev;

public sealed class KeycloakSyncApplicationService : IKeycloakSyncService
{
    private readonly IKeycloakSyncRunner _runner;

    public KeycloakSyncApplicationService(IKeycloakSyncRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    public async Task<SyncUsersResult> TriggerSyncAsync(CancellationToken cancellationToken = default)
    {
        await _runner.TriggerRunAsync(cancellationToken).ConfigureAwait(false);
        return new SyncUsersResult { Allowed = true, Triggered = true };
    }
}

