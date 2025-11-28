namespace KunstButikken.UserService.Application.Interfaces;

public interface IDbMigrationRunner
{
    Task RunMigrationsAsync(CancellationToken cancellationToken = default);
}
