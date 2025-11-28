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
    IDbMigrationRunner migrationRunner,
    IDevKeycloakSeeder devSeeder)
    : IKeycloakSeeder
{
    public Task ApplyMigrationsAsync(CancellationToken cancellationToken = default) =>
        migrationRunner.RunMigrationsAsync(cancellationToken);

    public Task SeedKeycloakUsersAsync(CancellationToken cancellationToken = default) =>
        devSeeder.SeedAsync(new SeedUsersRequest { Force = true }, cancellationToken);

    private static Uri BuildRealmRoleUri(string adminApiBase, string realm) => new(Uri.EscapeUriString($"{adminApiBase}/roles/{realm}"));

    // ...existing code...
}
