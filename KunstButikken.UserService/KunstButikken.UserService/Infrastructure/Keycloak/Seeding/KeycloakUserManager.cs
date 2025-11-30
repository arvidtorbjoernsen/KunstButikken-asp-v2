using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KunstButikken.UserService.Infrastructure.Keycloak.Seeding;

public sealed class KeycloakUserManager
{
    private readonly Uri _adminApiBaseUri;
    private readonly string _adminToken;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly Dictionary<string, (string id, string name)> _roleCache = new(StringComparer.OrdinalIgnoreCase);

    public KeycloakUserManager(Uri adminApiBaseUri, HttpClient httpClient, string adminToken, ILogger logger)
    {
        _adminApiBaseUri = adminApiBaseUri ?? throw new ArgumentNullException(nameof(adminApiBaseUri));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _adminToken = adminToken ?? throw new ArgumentNullException(nameof(adminToken));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static Uri BuildApiUri(Uri baseUri, string relative)
    {
        // Safe combine that preserves the base path and appends the relative path
        var baseStr = baseUri.ToString().TrimEnd('/');
        var rel = relative.StartsWith('/') ? relative : "/" + relative;
        return new Uri(baseStr + rel, UriKind.Absolute);
    }

    public async Task<(string id, string name)?> GetRoleAsync(string roleName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return null;
        }

        if (_roleCache.TryGetValue(roleName, out var cached))
        {
            return cached;
        }

        var roleUrl = BuildApiUri(_adminApiBaseUri, $"/roles/{Uri.EscapeDataString(roleName)}");
        using var req = new HttpRequestMessage(HttpMethod.Get, roleUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        using var res = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to resolve role {Role}: {Status} {Reason}", roleName, (int)res.StatusCode,
                res.ReasonPhrase);
            return null;
        }

        var json = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var id = doc.RootElement.GetProperty("id").GetString() ?? string.Empty;
        var name = doc.RootElement.GetProperty("name").GetString() ?? roleName;
        _roleCache[roleName] = (id, name);
        return (id, name);
    }

    public async Task<string?> FindUserIdByUsernameAsync(string username, CancellationToken ct = default)
    {
        var findUrl = BuildApiUri(_adminApiBaseUri, $"/users?username={Uri.EscapeDataString(username)}");
        using var req = new HttpRequestMessage(HttpMethod.Get, findUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        using var res = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();
        var listJson = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var users = JsonSerializer.Deserialize<List<JsonElement>>(listJson) ?? new List<JsonElement>();
        return users.Count > 0 ? users[0].GetProperty("id").GetString() : null;
    }

    public async Task<string?> CreateUserAsync(string username, CancellationToken ct = default)
    {
        var createUrl = BuildApiUri(_adminApiBaseUri, "/users");
        using var req = new HttpRequestMessage(HttpMethod.Post, createUrl);

        var payload = new
        {
            username,
            enabled = true,
            email = $"{username}@example.com",
            firstName = username.Split('-', '_').FirstOrDefault() ?? username,
            lastName = "User"
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        req.Content = content;
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        using var res = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
        if (res.StatusCode == HttpStatusCode.Conflict)
        {
            // another client created it concurrently; try to find it
            return await FindUserIdByUsernameAsync(username, ct).ConfigureAwait(false);
        }

        res.EnsureSuccessStatusCode();
        var loc = res.Headers.Location?.ToString();
        if (!string.IsNullOrWhiteSpace(loc))
        {
            var parts = loc.Split('/');
            var id = parts.LastOrDefault();
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }

        // fallback: attempt to query users
        return await FindUserIdByUsernameAsync(username, ct).ConfigureAwait(false);
    }

    public async Task SetPasswordAsync(string userId, string password, CancellationToken ct = default)
    {
        var pwdUrl = BuildApiUri(_adminApiBaseUri, $"/users/{userId}/reset-password");
        using var req = new HttpRequestMessage(HttpMethod.Put, pwdUrl);
        var pwdPayload = new { type = "password", value = password, temporary = false };
        using var content = new StringContent(JsonSerializer.Serialize(pwdPayload), Encoding.UTF8, "application/json");
        req.Content = content;
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        using var res = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();
    }

    public async Task<bool> MapRolesAsync(string userId, IEnumerable<(string id, string name)> roles,
        CancellationToken ct = default)
    {
        var list = roles.ToList();
        if (list.Count == 0)
        {
            return true;
        }

        var mapUrl = BuildApiUri(_adminApiBaseUri, $"/users/{userId}/role-mappings/realm");
        using var req = new HttpRequestMessage(HttpMethod.Post, mapUrl);
        using var content = new StringContent(JsonSerializer.Serialize(list.Select(r => new { r.id, r.name })),
            Encoding.UTF8, "application/json");
        req.Content = content;
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        using var res = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode && res.StatusCode != HttpStatusCode.Conflict)
        {
            _logger.LogWarning("Role mapping failed for user {UserId}: {Status} {Reason}", userId, (int)res.StatusCode,
                res.ReasonPhrase);
            return false;
        }

        return true;
    }
}
