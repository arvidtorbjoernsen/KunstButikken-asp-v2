namespace KunstButikken.UserService.Application.Services;

using KunstButikken.UserService.Application.Interfaces;
using KunstButikken.UserService.Shared.Dev;
using Microsoft.Extensions.Hosting;

public sealed class DevSeedService : IDevSeedService
{
    private readonly IHostEnvironment _env;
    private readonly IDevKeycloakSeeder _seeder;

    public DevSeedService(IHostEnvironment env, IDevKeycloakSeeder seeder)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _seeder = seeder ?? throw new ArgumentNullException(nameof(seeder));
    }

    public bool IsAllowed() => _env.IsDevelopment();

    public Task<SeedUsersResult> SeedKeycloakUsersAsync(SeedUsersRequest? request, CancellationToken cancellationToken = default)
    {
        return _seeder.SeedAsync(request, cancellationToken);
    }
}
