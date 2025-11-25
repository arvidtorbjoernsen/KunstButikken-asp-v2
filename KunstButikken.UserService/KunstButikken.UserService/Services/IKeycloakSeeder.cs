namespace KunstButikken.UserService.Services;

public interface IKeycloakSeeder
{
  Task ApplyMigrationsAsync(CancellationToken cancellationToken = default);
  Task SeedKeycloakUsersAsync(CancellationToken cancellationToken = default);
}