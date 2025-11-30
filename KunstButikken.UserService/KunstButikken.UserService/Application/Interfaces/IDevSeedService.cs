namespace KunstButikken.UserService.Application.Interfaces;

using KunstButikken.UserService.Shared.Dev;

public interface IDevSeedService
{
    bool IsAllowed();
    Task<SeedUsersResult> SeedKeycloakUsersAsync(SeedUsersRequest? request, CancellationToken cancellationToken = default);
}
