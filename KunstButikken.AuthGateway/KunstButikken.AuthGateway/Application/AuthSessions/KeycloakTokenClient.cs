using System.Net.Http.Json;

using KunstButikken.AuthGateway.Domain;

using Microsoft.Extensions.Configuration;

namespace KunstButikken.AuthGateway.Application.AuthSessions;

public sealed record KeycloakTokenResponse(
    string access_token,
    string refresh_token,
    int expires_in,
    int refresh_expires_in,
    string? id_token
);

public interface IKeycloakTokenClient
{
    Task<KeycloakTokenResponse?> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default);
    Task<KeycloakTokenResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public sealed class KeycloakTokenClient(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration
) : IKeycloakTokenClient
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<KeycloakTokenResponse?> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, string> { ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = redirectUri, ["client_id"] = _configuration["KEYCLOAK_CLIENT_ID"] ?? "kunstbutikken-frontend" };
        var secret = _configuration["KEYCLOAK_CLIENT_SECRET"];
        if (!string.IsNullOrEmpty(secret))
        {
            payload["client_secret"] = secret;
        }

        using var client = _httpClientFactory.CreateClient(nameof(KeycloakTokenClient));
        using var content = new FormUrlEncodedContent(payload);
        var response = await client.PostAsync(_configuration["KEYCLOAK_TOKEN_ENDPOINT"], content, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken).ConfigureAwait(false);
    }

    public async Task<KeycloakTokenResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["refresh_token"] = refreshToken, ["client_id"] = _configuration["KEYCLOAK_CLIENT_ID"] ?? "kunstbutikken-frontend" };
        var secret = _configuration["KEYCLOAK_CLIENT_SECRET"];
        if (!string.IsNullOrEmpty(secret))
        {
            payload["client_secret"] = secret;
        }

        return await SendAsync(payload, cancellationToken).ConfigureAwait(false);
    }

    private async Task<KeycloakTokenResponse?> SendAsync(Dictionary<string, string> payload, CancellationToken cancellationToken)
    {
        var endpoint = _configuration["KEYCLOAK_TOKEN_ENDPOINT"];
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new InvalidOperationException("KEYCLOAK_TOKEN_ENDPOINT must be configured");
        }

        var client = _httpClientFactory.CreateClient(nameof(KeycloakTokenClient));
        using var content = new FormUrlEncodedContent(payload);
        var response = await client.PostAsync(endpoint, content, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
