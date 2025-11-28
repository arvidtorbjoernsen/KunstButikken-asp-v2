namespace KunstButikken.UserService.Services;

public interface IKeycloakSyncRunner
{
  Task RunOnceAsync(CancellationToken ct = default);
  Task TriggerRunAsync(CancellationToken ct = default);
}