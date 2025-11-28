namespace KunstButikken.UserService.Application.Interfaces;

using KunstButikken.UserService.Shared.Dev;

public interface IDevKeycloakSeeder
{
    Task<SeedUsersResult> SeedAsync(SeedUsersRequest? request, CancellationToken cancellationToken = default);
}

