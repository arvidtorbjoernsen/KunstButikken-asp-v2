using KunstButikken.UserService.Controllers;

namespace KunstButikken.UserService.Services;

public sealed class DevSeedService
{
    private readonly IConfiguration _cfg;
    private readonly IWebHostEnvironment _env;
    private readonly KeycloakSeederService _keycloakSeeder;
    private readonly ILogger<DevSeedService> _logger;

    public DevSeedService(IConfiguration cfg, IHttpClientFactory httpFactory, IWebHostEnvironment env,
        ILogger<DevSeedService> logger, KeycloakSeederService keycloakSeeder)
    {
        _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        // keep httpFactory parameter for compatibility with existing registrations; not stored here
        _ = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keycloakSeeder = keycloakSeeder ?? throw new ArgumentNullException(nameof(keycloakSeeder));
    }

    public bool IsAllowed() => _env.IsDevelopment() ||
                               string.Equals(_cfg["ALLOW_DEV_SEED"], "true", StringComparison.OrdinalIgnoreCase);

    public async Task<SeedUsersResult> SeedKeycloakUsersAsync(SeedUsersRequest? req, CancellationToken ct = default)
    {
        // Delegate the heavy lifting to the extracted Keycloak seeder
        return await _keycloakSeeder.SeedKeycloakUsersAsync(req, ct).ConfigureAwait(false);
    }
}
