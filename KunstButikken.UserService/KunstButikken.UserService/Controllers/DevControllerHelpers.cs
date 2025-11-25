using System.Text.Json;

namespace KunstButikken.UserService.Controllers;

internal static class DevControllerHelpers
{
    // Default set of users used by dev seed flows. Kept here to be shared between services/controllers.
    internal static readonly (string Username, string[] Roles)[] DefaultDesiredUsers =
        new[]
        {
            ("seller1", new[] { "seller" }), ("seller2", new[] { "seller" }), ("seller3", new[] { "seller" }),
            ("buyer1", new[] { "buyer" }), ("buyer2", new[] { "buyer" }), ("buyer3", new[] { "buyer" }),
            ("buyer4", new[] { "buyer" }), ("buyer5", new[] { "buyer" }), ("buyer6", new[] { "buyer" }),
            ("buyer7", new[] { "buyer" }), ("app-admin", new[] { "admin" })
        };

    public static bool HasScheme(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        if (s.Contains("://", StringComparison.Ordinal))
        {
            return true;
        }

        if (Uri.TryCreate(s, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Scheme))
        {
            return true;
        }

        return false;
    }

    public static string Combine(string a, string b)
    {
        if (string.IsNullOrEmpty(a))
        {
            return b;
        }

        if (string.IsNullOrEmpty(b))
        {
            return a;
        }

        return a.TrimEnd('/') + "/" + b.TrimStart('/');
    }

    // Uri overload to avoid switching back to string-heavy arguments in callers
    public static string Combine(Uri baseUri, string relative)
    {
        if (baseUri == null)
        {
            throw new ArgumentNullException(nameof(baseUri));
        }

        if (string.IsNullOrEmpty(relative))
        {
            return baseUri.ToString();
        }

        // If relative contains a scheme, return it as-is
        if (HasScheme(relative))
        {
            return relative;
        }

        try
        {
            if (relative.StartsWith('/'))
            {
                var baseAuthority = $"{baseUri.Scheme}://{baseUri.Authority}";
                var ret = baseAuthority + relative.TrimEnd('/');
                if (!HasScheme(ret) && ret.StartsWith('/'))
                {
                    ret = baseAuthority + ret;
                }

                return ret;
            }

            var combined = new Uri(baseUri, relative);
            var ret2 = combined.ToString().TrimEnd('/');
            if (!HasScheme(ret2) && ret2.StartsWith('/'))
            {
                ret2 = $"{baseUri.Scheme}://{baseUri.Authority}" + ret2;
            }

            return ret2;
        }
        catch
        {
            // Fallback to concatenation
            string baseStr;
            try
            {
                baseStr = baseUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            }
            catch
            {
                baseStr = baseUri.ToString().TrimEnd('/');
            }

            var ret3 = baseStr + "/" + relative.TrimStart('/');
            if (!HasScheme(ret3) && ret3.StartsWith('/'))
            {
                ret3 = $"{baseUri.Scheme}://{baseUri.Authority}" + ret3;
            }

            return ret3;
        }
    }

    public static string? ResolveRealmFromIssuer(string issuer)
    {
        if (string.IsNullOrWhiteSpace(issuer))
        {
            return null;
        }

        var idx = issuer.IndexOf("/realms/", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            return issuer[(idx + "/realms/".Length)..];
        }

        return null;
    }

    public static string? ResolveAdminBase(string issuer, string realm)
    {
        if (string.IsNullOrWhiteSpace(issuer))
        {
            return null;
        }

        var toRemove = $"/realms/{realm}";
        if (issuer.EndsWith(toRemove, StringComparison.OrdinalIgnoreCase))
        {
            return issuer[..^toRemove.Length];
        }

        try
        {
            var uri = new Uri(issuer);
            return uri.GetLeftPart(UriPartial.Authority);
        }
        catch
        {
            return null;
        }
    }

    public static string[] InferRoles(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            return [];
        }

        if (username.StartsWith("seller", StringComparison.OrdinalIgnoreCase))
        {
            return ["seller"];
        }

        if (username.StartsWith("buyer", StringComparison.OrdinalIgnoreCase))
        {
            return ["buyer"];
        }

        if (username.Contains("admin", StringComparison.OrdinalIgnoreCase))
        {
            return ["admin"];
        }

        return [];
    }

    // Acquire admin token using grouped config to avoid primitive-argument passing in callers
    public static async Task<string?> AcquireAdminTokenAsync(HttpClient client, KeycloakAdminConfig cfg)
    {
        if (client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (cfg is null)
        {
            throw new ArgumentNullException(nameof(cfg));
        }

        var tokenClient = new KeycloakTokenClient(client, cfg.TokenEndpoint);
        var creds = cfg.Credentials;

        if (HasClientCredentials(creds))
        {
            return await tokenClient.GetClientCredentialsTokenAsync(new ClientCredentials(
                creds.AdminClientId ?? string.Empty, // Added null-coalescing operator
                creds.AdminClientSecret ?? string.Empty)).ConfigureAwait(false);
        }

        if (HasPasswordCredentials(creds))
        {
            return await tokenClient.GetPasswordGrantTokenAsync(new PasswordGrant(creds.AdminClientId ?? string.Empty,
                creds.AdminUsername ?? string.Empty, creds.AdminPassword ?? string.Empty)).ConfigureAwait(false);
        }

        return null;
    }

    private static bool HasClientCredentials(KeycloakCredentials? creds)
    {
        return creds is not null && !string.IsNullOrWhiteSpace(creds.AdminClientId) &&
               !string.IsNullOrWhiteSpace(creds.AdminClientSecret);
    }

    private static bool HasPasswordCredentials(KeycloakCredentials? creds)
    {
        return creds is not null && !string.IsNullOrWhiteSpace(creds.AdminUsername) &&
               !string.IsNullOrWhiteSpace(creds.AdminPassword);
    }

    // Typed request objects to avoid primitive obsession for token requests
    internal record ClientCredentials(string ClientId, string ClientSecret);

    internal record PasswordGrant(string ClientId, string Username, string Password);

    // Group credentials to avoid passing many primitive strings around
    internal record KeycloakCredentials(
        string? AdminClientId,
        string? AdminClientSecret,
        string? AdminUsername,
        string? AdminPassword);

    // Configuration grouping for Keycloak admin parameters to reduce primitive-argument passing
    // Keycloak admin configuration (positional):
    //   AdminBase: base URL to the Keycloak installation (e.g. http://keycloak)
    //   TokenRealm: realm used for token endpoint (often 'master')
    //   TokenEndpoint: full token endpoint URI (absolute)
    //   Credentials: admin credentials grouping
    //   Realm: target realm for admin API operations
    internal record KeycloakAdminConfig(
        string AdminBase,
        string TokenRealm,
        Uri TokenEndpoint,
        KeycloakCredentials Credentials,
        string Realm);

    // Small instance wrapper that holds HttpClient + token endpoint, so methods don't take many primitive args
    internal sealed class KeycloakTokenClient(HttpClient client, Uri tokenEndpoint)
    {
        private readonly HttpClient _client = client ?? throw new ArgumentNullException(nameof(client));
        private readonly Uri _tokenEndpoint = tokenEndpoint ?? throw new ArgumentNullException(nameof(tokenEndpoint));

        public async Task<string?> GetClientCredentialsTokenAsync(ClientCredentials creds)
        {
            if (creds is null)
            {
                throw new ArgumentNullException(nameof(creds));
            }

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = creds.ClientId,
                ["client_secret"] = creds.ClientSecret
            };

            return await PostFormAndExtractTokenAsync(form).ConfigureAwait(false);
        }

        public async Task<string?> GetPasswordGrantTokenAsync(PasswordGrant creds)
        {
            if (creds is null)
            {
                throw new ArgumentNullException(nameof(creds));
            }

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = string.IsNullOrWhiteSpace(creds.ClientId) ? "admin-cli" : creds.ClientId,
                ["username"] = creds.Username,
                ["password"] = creds.Password
            };

            return await PostFormAndExtractTokenAsync(form).ConfigureAwait(false);
        }

        private async Task<string?> PostFormAndExtractTokenAsync(Dictionary<string, string> form)
        {
            using var content = new FormUrlEncodedContent(form);
            // Use Uri overload to construct request
            using var req = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
            req.Content = content;

            using var res = await _client.SendAsync(req).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("access_token", out var tok) ? tok.GetString() : null;
        }
    }

    // Small helper that groups admin config + HttpClient and exposes convenience methods used by DevController
    internal sealed class KeycloakAdminClient(KeycloakAdminConfig cfg, HttpClient client)
    {
        private readonly KeycloakAdminConfig _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        private readonly HttpClient _client = client ?? throw new ArgumentNullException(nameof(client));

        public string AdminBase => _cfg.AdminBase;
        public string Realm => _cfg.Realm;
        public Uri TokenEndpoint => _cfg.TokenEndpoint;
        public string TokenRealm => _cfg.TokenRealm;

        public string AdminApiBase => Combine(AdminBase, $"/admin/realms/{Realm}");

        public async Task<string?> AcquireAdminTokenAsync() =>
            await DevControllerHelpers.AcquireAdminTokenAsync(_client, _cfg).ConfigureAwait(false);
    }
}
