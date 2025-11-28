using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KunstButikken.UserService.Infrastructure.Persistence;
using KunstButikken.UserService.Application.Interfaces;
using KunstButikken.UserService.Shared.Dev;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.UserService.Infrastructure.Keycloak.Seeding;

public class KeycloakSeeder(
    ILogger<KeycloakSeeder> logger,
    IHttpClientFactory httpFactory,
    IConfiguration config,
    IServiceProvider services,
    IDbMigrationRunner migrationRunner)
    : IKeycloakSeeder
{
    private readonly ILogger<KeycloakSeeder> _logger = logger;
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly IConfiguration _config = config;
    private readonly IServiceProvider _services = services;
    private readonly IDbMigrationRunner _migrationRunner = migrationRunner;

    public Task ApplyMigrationsAsync(CancellationToken cancellationToken = default) =>
        _migrationRunner.RunMigrationsAsync(cancellationToken);

    public Task SeedKeycloakUsersAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var devSeeder = scope.ServiceProvider.GetRequiredService<IDevKeycloakSeeder>();
        return devSeeder.SeedAsync(new SeedUsersRequest { Force = true }, cancellationToken);
    }

    private static Uri BuildRealmRoleUri(string adminApiBase, string realm) => new(Uri.EscapeDataString($"{adminApiBase}/roles/{realm}"));

    // ...existing code...
}
