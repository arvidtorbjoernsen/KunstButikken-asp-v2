using KunstButikken.UserService.Application.Interfaces;
using Microsoft.Extensions.Http;
using KunstButikken.UserService.Shared.Dev;

namespace KunstButikken.UserService.Infrastructure.Keycloak.Seeding;

/// <summary>
///     Extracted Keycloak seeding logic from <see cref="DevSeedService" /> to reduce size and
///     make ownership of disposable objects explicit.
/// </summary>
public sealed class KeycloakSeederService : IDevKeycloakSeeder
{
    private readonly IConfiguration _cfg;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<KeycloakSeederService> _logger;
    private readonly KeycloakUserProcessor _processor;
    private readonly IHttpClientFactory _httpFactory;

    public KeycloakSeederService(IConfiguration cfg, IHttpClientFactory httpFactory, IWebHostEnvironment env,
        ILogger<KeycloakSeederService> logger, KeycloakUserProcessor processor)
    {
        _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
    }

    private HttpClient CreateClient() => _httpFactory.CreateClient(nameof(KeycloakSeederService));

    public bool IsAllowed() => _env.IsDevelopment() ||
                               string.Equals(_cfg["ALLOW_DEV_SEED"], "true", StringComparison.OrdinalIgnoreCase);

    public async Task<SeedUsersResult> SeedAsync(SeedUsersRequest? req, CancellationToken ct = default)
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

        var tokenService = new KeycloakTokenService(CreateClient());
        var adminToken = await tokenService.AcquireTokenAsync(adminConfig).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(adminToken))
        {
            result.Errors.Add("Failed to obtain Keycloak admin token. Check KEYCLOAK_ADMIN_* settings in AppHost.");
            result.Errors.Add($"Token endpoint: {adminConfig.TokenEndpoint}");
            return result;
        }

        var desired = req?.Users is { Count: > 0 }
            ? req!.Users!.Select(u => new { Username = u, Roles = DevControllerHelpers.InferRoles(u) }).ToList()
            : DevSeedDefaults.DefaultDesiredUsers.Select(u => new { u.Username, u.Roles }).ToList();

        var adminApiBaseUri =
            new Uri(DevControllerHelpers.Combine(adminConfig.AdminBase, $"/admin/realms/{adminConfig.Realm}"));
        var manager = new KeycloakUserManager(adminApiBaseUri, CreateClient(), adminToken, _logger);

        // Delegate per-user processing to a small collaborator to keep this method concise/testable.
        await _processor.ProcessUsersAsync(manager, desired, result, ct).ConfigureAwait(false);

        return result;
    }
}
