namespace KunstButikken.UserService.Services;

public interface IDbMigrationRunner
{
  Task RunMigrationsAsync(CancellationToken ct = default);
}