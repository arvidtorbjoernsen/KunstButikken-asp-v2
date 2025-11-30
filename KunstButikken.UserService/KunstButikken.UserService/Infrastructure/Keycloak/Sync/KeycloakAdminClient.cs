using System.Text.Json;
using KunstButikken.Common.Logging;

namespace KunstButikken.UserService.Infrastructure.Keycloak.Sync;

public record AdminTokenResult(
    string? Token,
    string? TokenEndpoint,
    string? TokenRealm,
    string? ClientId,
    string? GrantTried,
    string? HttpError);

// Grouped Keycloak admin configuration to avoid passing many string primitives around
// Replaced KeycloakAdminConfig record with interface-based design
// Internal implementation of IKeycloakAdminConfig to avoid exposing the concrete type
internal sealed class KeycloakAdminConfigImpl(
    string issuer,
    string realm,
    string adminBase,
    string tokenEndpoint,
    string tokenRealm,
    string adminClientId,
    string? adminClientSecret,
    string? adminUsername,
    string? adminPassword)
    : IKeycloakAdminConfig
{
    public string Issuer { get; } = issuer;
    public string Realm { get; } = realm;
    public string AdminBase { get; } = adminBase;
    public string TokenEndpoint { get; } = tokenEndpoint;
    public string TokenRealm { get; } = tokenRealm;
    public string AdminClientId { get; } = adminClientId;
    public string? AdminClientSecret { get; } = adminClientSecret;
    public string? AdminUsername { get; } = adminUsername;
    public string? AdminPassword { get; } = adminPassword;

    public bool HasClientCredentials =>
        !string.IsNullOrWhiteSpace(AdminClientId) && !string.IsNullOrWhiteSpace(AdminClientSecret);

    public bool HasPasswordGrant => !string.IsNullOrWhiteSpace(AdminClientId) &&
                                    !string.IsNullOrWhiteSpace(AdminUsername) &&
                                    !string.IsNullOrWhiteSpace(AdminPassword);
}

public class KeycloakAdminClient(IConfiguration config, ILogger<KeycloakAdminClient> logger) : IKeycloakAdminClient
{
    // LoggerMessage delegates (non-generic ILogger-based)
    private static readonly Action<ILogger, string, Exception?> _failedResolveConfig =
        Log.Define<string>(LogLevel.Warning, new EventId(2000, "FailedResolveConfig"),
            "KeycloakAdminClient: failed to resolve admin token config for issuer {Issuer}");

    private static readonly Action<ILogger, int, int, Exception?> _retryFailed =
        Log.Define<int, int>(LogLevel.Warning, new EventId(2001, "RetryFailed"),
            "KeycloakAdminClient: retry {Attempt}/{Attempts} failed");

    private static readonly Action<ILogger, int, Exception?> _operationFailedAfterAttempts =
        Log.Define<int>(LogLevel.Warning, new EventId(2002, "OperationFailedAfterAttempts"),
            "KeycloakAdminClient: operation failed after {Attempts} attempts");

    private static readonly Action<ILogger, string, Exception?> _tryingClientCreds =
        Log.Define<string>(LogLevel.Information, new EventId(2003, "TryingClientCreds"),
            "[KeycloakAdminClient] Trying client_credentials for {ClientId}");

    private static readonly Action<ILogger, string, Exception?> _tryingPasswordGrant =
        Log.Define<string>(LogLevel.Information, new EventId(2004, "TryingPasswordGrant"),
            "[KeycloakAdminClient] Trying password grant for {Username}");

    private static readonly Action<ILogger, int, string, Exception?> _tokenEndpointReturned =
        Log.Define<int, string>(LogLevel.Warning, new EventId(2005, "TokenEndpointReturned"),
            "KeycloakAdminClient: token endpoint returned {Status} {Reason}");

    private static readonly Action<ILogger, Exception?> _requestTokenFailed =
        Log.Define(LogLevel.Warning, new EventId(2006, "RequestTokenFailed"),
            "KeycloakAdminClient: RequestTokenAsync failed");

    public async Task<(string? Token, string? TokenEndpoint, string? TokenRealm, string? ClientId, string? GrantTried, string? HttpError)> TryGetAdminTokenAsync(HttpClient http, string issuer, CancellationToken ct)
    {
        var cfg = ResolveAdminTokenConfig(issuer);
        if (cfg == null)
        {
            _failedResolveConfig(logger, issuer, null);
            return (null, null, null, null, null, "Failed to resolve config");
        }

        // Try client_credentials first, then password grant using small helpers to reduce nesting
        var clientToken = await TryClientCredentialsAsync(cfg, http, ct).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(clientToken))
        {
            return (clientToken, cfg.TokenEndpoint, cfg.TokenRealm, cfg.AdminClientId, "client_credentials", null);
        }

        var pwdToken = await TryPasswordGrantAsync(cfg, http, ct).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(pwdToken))
        {
            return (pwdToken, cfg.TokenEndpoint, cfg.TokenRealm, cfg.AdminClientId, "password", null);
        }

        return (null, cfg.TokenEndpoint, cfg.TokenRealm, cfg.AdminClientId, null, "Failed to obtain token");
    }

    // Generic retry helper used by token acquisition to reduce duplicated retry logic
    private async Task<T?> RetryAsync<T>(Func<Task<T?>> op, CancellationToken ct, int attempts = 3,
        int delayBaseMs = 200)
        where T : class
    {
        for (var i = 0; i < attempts && !ct.IsCancellationRequested; i++)
        {
            try
            {
                var res = await op().ConfigureAwait(false);
                if (res != null)
                {
                    return res;
                }
            }
            catch (Exception ex)
            {
                _retryFailed(logger, i + 1, attempts, ex);
            }

            await Task.Delay(delayBaseMs * (i + 1), ct).ConfigureAwait(false);
        }

        _operationFailedAfterAttempts(logger, attempts, null);
        return null;
    }

    private async Task<string?> TryClientCredentialsAsync(KeycloakAdminConfigImpl cfg, HttpClient http,
        CancellationToken ct)
    {
        if (!cfg.HasClientCredentials)
        {
            return null;
        }

        _tryingClientCreds(logger, cfg.AdminClientId, null);
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials", ["client_id"] = cfg.AdminClientId
        };
        if (!string.IsNullOrWhiteSpace(cfg.AdminClientSecret))
        {
            body["client_secret"] = cfg.AdminClientSecret!;
        }

        return await RetryAsync(() => RequestTokenAsync(http, cfg.TokenEndpoint, body, ct), ct).ConfigureAwait(false);
    }

    private async Task<string?> TryPasswordGrantAsync(KeycloakAdminConfigImpl cfg, HttpClient http,
        CancellationToken ct)
    {
        if (!cfg.HasPasswordGrant)
        {
            return null;
        }

        _tryingPasswordGrant(logger, cfg.AdminUsername ?? string.Empty, null);
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "password", ["client_id"] = cfg.AdminClientId, ["username"] = cfg.AdminUsername ?? string.Empty, ["password"] = cfg.AdminPassword ?? string.Empty
        };
        if (!string.IsNullOrWhiteSpace(cfg.AdminClientSecret))
        {
            body["client_secret"] = cfg.AdminClientSecret!;
        }

        return await RetryAsync(() => RequestTokenAsync(http, cfg.TokenEndpoint, body, ct), ct).ConfigureAwait(false);
    }

    // Consolidated HTTP POST for token acquisition to remove duplicated parsing logic
    private async Task<string?> RequestTokenAsync(HttpClient http, string tokenEndpoint,
        Dictionary<string, string> body,
        CancellationToken ct)
    {
        try
        {
            var endpointUri = new Uri(tokenEndpoint);
            // Ensure FormUrlEncodedContent is disposed after use to avoid CA2000 warning
            using var content = new FormUrlEncodedContent(body);
            var res = await http.PostAsync(endpointUri, content, ct).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
            {
                _tokenEndpointReturned(logger, (int)res.StatusCode, res.ReasonPhrase ?? string.Empty, null);
                return null;
            }

            // Await the ReadAsStreamAsync task with ConfigureAwait(false) to get a Stream.
            var st = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            // Explicitly dispose asynchronously with ConfigureAwait(false) in a finally block so
            // the stream passed to ParseAsync is an actual Stream (not a ConfiguredAsyncDisposable).
            try
            {
                var json = await JsonDocument.ParseAsync(st, cancellationToken: ct).ConfigureAwait(false);
                return json.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : null;
            }
            finally
            {
                // Ensure we configure the await on DisposeAsync as requested.
                await st.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _requestTokenFailed(logger, ex);
            return null;
        }
    }

    private KeycloakAdminConfigImpl? ResolveAdminTokenConfig(string issuer)
    {
        var targetRealm = ResolveRealmFromIssuer(issuer) ?? config["KEYCLOAK_REALM"] ?? string.Empty;
        var tokenRealm = config["KEYCLOAK_ADMIN_TOKEN_REALM"] ?? "master";
        var adminBase = GetAdminBaseForIssuer(issuer, targetRealm);
        if (string.IsNullOrWhiteSpace(adminBase))
        {
            return null;
        }

        var tokenEndpoint = $"{adminBase.TrimEnd('/')}/realms/{tokenRealm}/protocol/openid-connect/token";

        var adminClientId = config["KEYCLOAK_ADMIN_CLIENT_ID"] ?? config["KEYCLOAK_CLIENT_ID_ADMIN"] ?? string.Empty;
        var adminClientSecret = config["KEYCLOAK_ADMIN_CLIENT_SECRET"] ?? config["KEYCLOAK_CLIENT_SECRET_ADMIN"];
        var adminUsername = config["KC_BOOTSTRAP_ADMIN_USERNAME"] ??
                            config["KEYCLOAK_ADMIN_USERNAME"] ?? config["KEYCLOAK_ADMIN_USER"];
        var adminPassword = config["KC_BOOTSTRAP_ADMIN_PASSWORD"] ?? config["KEYCLOAK_ADMIN_PASSWORD"];

        if (string.IsNullOrWhiteSpace(adminClientId) && !string.IsNullOrWhiteSpace(adminUsername) &&
            !string.IsNullOrWhiteSpace(adminPassword))
        {
            adminClientId = "admin-cli";
        }

        return new KeycloakAdminConfigImpl(issuer, targetRealm, adminBase, tokenEndpoint, tokenRealm, adminClientId, adminClientSecret, adminUsername,
            adminPassword);
    }

    private string? GetAdminBaseForIssuer(string issuer, string realm)
    {
        var adminBase = ResolveAdminBase(issuer, realm) ?? issuer.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(issuer) && issuer.Contains("aspire.hosting.applicationmodel.endpointreference",
                StringComparison.OrdinalIgnoreCase))
        {
            var discoverableBase = config["KEYCLOAK_BASE"];
            adminBase = !string.IsNullOrWhiteSpace(discoverableBase)
                ? discoverableBase.TrimEnd('/')
                : "http://keycloak";
        }

        if (string.IsNullOrWhiteSpace(adminBase))
        {
            return null;
        }

        if (!HasScheme(adminBase))
        {
            adminBase = "http://" + adminBase.TrimStart('/');
        }

        return adminBase;
    }

    private static string? ResolveRealmFromIssuer(string issuer)
    {
        if (string.IsNullOrWhiteSpace(issuer))
        {
            return null;
        }

        var idx = issuer.IndexOf("/realms/", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? issuer[(idx + 8)..] : null;
    }

    private static string? ResolveAdminBase(string issuer, string realm)
    {
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(realm))
        {
            return null;
        }

        var suffix = $"/realms/{realm}";
        if (issuer.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return issuer[..^suffix.Length];
        }

        try
        {
            return new Uri(issuer).GetLeftPart(UriPartial.Authority);
        }
        catch
        {
            return null;
        }
    }

    private static bool HasScheme(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        if (s.Contains("://", StringComparison.Ordinal))
        {
            return true;
        }

        return Uri.TryCreate(s, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Scheme);
    }
}
