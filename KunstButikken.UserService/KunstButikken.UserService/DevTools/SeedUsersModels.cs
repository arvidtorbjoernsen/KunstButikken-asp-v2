namespace KunstButikken.UserService.Shared.Dev;

using System.Collections.ObjectModel;
using System.Text.Json;

public static class DevSeedDefaults
{
    public static readonly (string Username, string[] Roles)[] DefaultDesiredUsers =
    [
        ("seller1", ["seller"]),
        ("seller2", ["seller"]),
        ("seller3", ["seller"]),
        ("buyer1", ["buyer"]),
        ("buyer2", ["buyer"]),
        ("buyer3", ["buyer"]),
        ("buyer4", ["buyer"]),
        ("buyer5", ["buyer"]),
        ("buyer6", ["buyer"]),
        ("buyer7", ["buyer"]),
        ("app-admin", ["admin"])
    ];
}

public class SeedUsersRequest
{
    public bool Force { get; set; }
    public string? Realm { get; set; }
    public Collection<string> Users { get; } = new();
}

public class SeedUsersResult
{
    public bool Allowed { get; set; }
    public string Realm { get; set; } = string.Empty;
    public Collection<string> Created { get; } = new();
    public Collection<string> Skipped { get; } = new();
    public Collection<string> Errors { get; } = new();
}

public class SyncUsersResult
{
    public bool Allowed { get; set; }
    public bool Triggered { get; set; }
    public string? LastRunError { get; set; }
    public bool? LastRunSucceeded { get; set; }
    public DateTimeOffset? LastRunUtc { get; set; }
    public TimeSpan? LastRunDuration { get; set; }
    public string? LastAuthTokenEndpoint { get; set; }
    public string? LastAuthTokenRealm { get; set; }
    public string? LastAuthClientId { get; set; }
    public string? LastAuthGrant { get; set; }
    public string? LastAuthHttpError { get; set; }
}

public static class DevControllerHelpers
{
    public static bool HasScheme(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        if (s.Contains("://", StringComparison.Ordinal)) return true;
        return Uri.TryCreate(s, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Scheme);
    }

    public static string Combine(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b;
        if (string.IsNullOrEmpty(b)) return a;
        return a.TrimEnd('/') + "/" + b.TrimStart('/');
    }

    public static string Combine(Uri baseUri, string relative)
    {
        if (baseUri == null) throw new ArgumentNullException(nameof(baseUri));
        if (string.IsNullOrEmpty(relative)) return baseUri.ToString();
        if (HasScheme(relative)) return relative;
        try
        {
            if (relative.StartsWith('/'))
            {
                var baseAuthority = $"{baseUri.Scheme}://{baseUri.Authority}";
                var ret = baseAuthority + relative.TrimEnd('/');
                if (!HasScheme(ret) && ret.StartsWith('/')) ret = baseAuthority + ret;
                return ret;
            }

            var combined = new Uri(baseUri, relative);
            var ret2 = combined.ToString().TrimEnd('/');
            if (!HasScheme(ret2) && ret2.StartsWith('/')) ret2 = $"{baseUri.Scheme}://{baseUri.Authority}" + ret2;
            return ret2;
        }
        catch
        {
            string baseStr;
            try { baseStr = baseUri.GetLeftPart(UriPartial.Authority).TrimEnd('/'); }
            catch { baseStr = baseUri.ToString().TrimEnd('/'); }
            var ret3 = baseStr + "/" + relative.TrimStart('/');
            if (!HasScheme(ret3) && ret3.StartsWith('/')) ret3 = $"{baseUri.Scheme}://{baseUri.Authority}" + ret3;
            return ret3;
        }
    }

    public static string? ResolveRealmFromIssuer(string issuer)
    {
        if (string.IsNullOrWhiteSpace(issuer)) return null;
        var idx = issuer.IndexOf("/realms/", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? issuer[(idx + "/realms/".Length)..] : null;
    }

    public static string? ResolveAdminBase(string issuer, string realm)
    {
        if (string.IsNullOrWhiteSpace(issuer)) return null;
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
        if (string.IsNullOrEmpty(username)) return Array.Empty<string>();
        if (username.StartsWith("seller", StringComparison.OrdinalIgnoreCase)) return ["seller"];
        if (username.StartsWith("buyer", StringComparison.OrdinalIgnoreCase)) return ["buyer"];
        if (username.Contains("admin", StringComparison.OrdinalIgnoreCase)) return ["admin"];
        return Array.Empty<string>();
    }

    public static async Task<string?> AcquireAdminTokenAsync(HttpClient client, KeycloakAdminConfig cfg)
    {
        if (client is null) throw new ArgumentNullException(nameof(client));
        if (cfg is null) throw new ArgumentNullException(nameof(cfg));

        var tokenClient = new KeycloakTokenClient(client, cfg.TokenEndpoint);
        var creds = cfg.Credentials;

        if (HasClientCredentials(creds))
        {
            return await tokenClient.GetClientCredentialsTokenAsync(new ClientCredentials(
                creds.AdminClientId ?? string.Empty,
                creds.AdminClientSecret ?? string.Empty)).ConfigureAwait(false);
        }

        if (HasPasswordCredentials(creds))
        {
            return await tokenClient.GetPasswordGrantTokenAsync(new PasswordGrant(
                creds.AdminClientId ?? string.Empty,
                creds.AdminUsername ?? string.Empty,
                creds.AdminPassword ?? string.Empty)).ConfigureAwait(false);
        }

        return null;
    }

    private static bool HasClientCredentials(KeycloakCredentials? creds) =>
        creds is not null && !string.IsNullOrWhiteSpace(creds.AdminClientId) && !string.IsNullOrWhiteSpace(creds.AdminClientSecret);

    private static bool HasPasswordCredentials(KeycloakCredentials? creds) =>
        creds is not null && !string.IsNullOrWhiteSpace(creds.AdminUsername) && !string.IsNullOrWhiteSpace(creds.AdminPassword);

    public record ClientCredentials(string ClientId, string ClientSecret);
    public record PasswordGrant(string ClientId, string Username, string Password);

    public record KeycloakCredentials(
        string? AdminClientId,
        string? AdminClientSecret,
        string? AdminUsername,
        string? AdminPassword);

    public record KeycloakAdminConfig(
        string AdminBase,
        string TokenRealm,
        Uri TokenEndpoint,
        KeycloakCredentials Credentials,
        string Realm);

    public sealed class KeycloakTokenClient(HttpClient client, Uri tokenEndpoint)
    {
        private readonly HttpClient _client = client ?? throw new ArgumentNullException(nameof(client));
        private readonly Uri _tokenEndpoint = tokenEndpoint ?? throw new ArgumentNullException(nameof(tokenEndpoint));

        public async Task<string?> GetClientCredentialsTokenAsync(ClientCredentials creds)
        {
            if (creds is null) throw new ArgumentNullException(nameof(creds));

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
            if (creds is null) throw new ArgumentNullException(nameof(creds));

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
            using var req = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
            {
                Content = content
            };
            using var res = await _client.SendAsync(req).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("access_token", out var tok) ? tok.GetString() : null;
        }
    }
}

