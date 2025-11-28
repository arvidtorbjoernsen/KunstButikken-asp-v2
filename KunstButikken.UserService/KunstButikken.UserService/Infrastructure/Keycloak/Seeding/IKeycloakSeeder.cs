namespace KunstButikken.UserService.Infrastructure.Keycloak.Seeding;

public interface IKeycloakSeeder
{
    Task ApplyMigrationsAsync(CancellationToken cancellationToken = default);
    Task SeedKeycloakUsersAsync(CancellationToken cancellationToken = default);
}
