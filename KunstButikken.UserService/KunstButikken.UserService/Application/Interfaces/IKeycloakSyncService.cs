namespace KunstButikken.UserService.Application.Interfaces;

using KunstButikken.UserService.Shared.Dev;

public interface IKeycloakSyncService
{
    Task<SyncUsersResult> TriggerSyncAsync(CancellationToken cancellationToken = default);
}
