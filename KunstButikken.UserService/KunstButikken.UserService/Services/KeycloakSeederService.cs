using KunstButikken.UserService.Controllers;

namespace KunstButikken.UserService.Services;

/// <summary>
///     Extracted Keycloak seeding logic from <see cref="DevSeedService" /> to reduce size and
///     make ownership of disposable objects explicit.
/// </summary>
public sealed class KeycloakSeederService
{
    private readonly IConfiguration _cfg;
    private readonly IWebHostEnvironment _env;
    private readonly HttpClient _httpClient;
    private readonly ILogger<KeycloakSeederService> _logger;
    private readonly KeycloakUserProcessor _processor;

    // Preferred: receive an HttpClient via DI (AddHttpClient<KeycloakSeederService>) so the client
    // lifetime is managed by the framework and static analyzers won't flag disposal issues.
    public KeycloakSeederService(IConfiguration cfg, HttpClient httpClient, IWebHostEnvironment env,
        ILogger<KeycloakSeederService> logger, KeycloakUserProcessor processor)
    {
        _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
    }

    // Back-compat constructor: keep factory-based construction but prefer DI HttpClient.
    public KeycloakSeederService(IConfiguration cfg, IHttpClientFactory httpFactory, IWebHostEnvironment env,
        ILogger<KeycloakSeederService> logger, KeycloakUserProcessor processor)
        : this(cfg, httpFactory?.CreateClient() ?? throw new ArgumentNullException(nameof(httpFactory)), env, logger,
            processor)
    {
        // Intentionally empty - delegates to primary ctor
    }

    public bool IsAllowed() => _env.IsDevelopment() ||
                               string.Equals(_cfg["ALLOW_DEV_SEED"], "true", StringComparison.OrdinalIgnoreCase);

    public async Task<SeedUsersResult> SeedKeycloakUsersAsync(SeedUsersRequest? req, CancellationToken ct = default)
    {
        var result = new SeedUsersResult { Allowed = IsAllowed() };
        if (!result.Allowed)
        {
            return result;
        }

        // Resolve Keycloak configuration and create HttpClient
        var adminConfig = KeycloakConfigResolver.Resolve(_cfg, req);
        if (adminConfig is null)
        {
            result.Errors.Add("Unable to resolve Keycloak admin configuration from environment");
            return result;
        }

        var tokenService = new KeycloakTokenService(_httpClient);
        var adminToken = await tokenService.AcquireTokenAsync(adminConfig).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(adminToken))
        {
            result.Errors.Add("Failed to obtain Keycloak admin token. Check KEYCLOAK_ADMIN_* settings in AppHost.");
            result.Errors.Add($"Token endpoint: {adminConfig.TokenEndpoint}");
            return result;
        }

        var desired = req?.Users is { Count: > 0 }
            ? req!.Users!.Select(u => new { Username = u, Roles = DevControllerHelpers.InferRoles(u) }).ToList()
            : DevControllerHelpers.DefaultDesiredUsers.Select(u => new { u.Username, u.Roles }).ToList();

        var adminApiBaseUri =
            new Uri(DevControllerHelpers.Combine(adminConfig.AdminBase, $"/admin/realms/{adminConfig.Realm}"));
        var manager = new KeycloakUserManager(adminApiBaseUri, _httpClient, adminToken, _logger);

        // Delegate per-user processing to a small collaborator to keep this method concise/testable.
        await _processor.ProcessUsersAsync(manager, desired, result, ct).ConfigureAwait(false);

        return result;
    }
}
