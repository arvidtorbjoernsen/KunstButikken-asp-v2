namespace KunstButikken.UserService.Application.Interfaces;

public interface IKeycloakSyncRunner
{
    Task RunOnceAsync(CancellationToken cancellationToken = default);
    Task TriggerRunAsync(CancellationToken cancellationToken = default);
}
