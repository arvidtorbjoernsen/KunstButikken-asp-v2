namespace KunstButikken.UserService.Application.Interfaces;

public interface IKeycloakService
{
    Task SyncUserAsync(Guid userId, CancellationToken ct = default);
}

