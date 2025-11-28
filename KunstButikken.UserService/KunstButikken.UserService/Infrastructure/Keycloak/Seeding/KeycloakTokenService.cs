using System.Diagnostics.CodeAnalysis;
using KunstButikken.UserService.Shared.Dev;

namespace KunstButikken.UserService.Infrastructure.Keycloak.Seeding;

/// <summary>
///     Small wrapper to acquire admin token using an existing HttpClient. Keeps token-acquisition concerns isolated.
/// </summary>
internal sealed class KeycloakTokenService
{
    private readonly HttpClient _client;

    public KeycloakTokenService(HttpClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    [SuppressMessage("Usage", "CA2000:Dispose objects before losing scope",
        Justification =
            "FormUrlEncodedContent and HttpRequestMessage are disposed by using statements inside KeycloakTokenClient.")]
    public async Task<string?> AcquireTokenAsync(DevControllerHelpers.KeycloakAdminConfig cfg)
    {
        if (cfg is null)
        {
            throw new ArgumentNullException(nameof(cfg));
        }

        var tokenClient = new DevControllerHelpers.KeycloakTokenClient(_client, cfg.TokenEndpoint);
        var creds = cfg.Credentials;

        if (creds is not null && !string.IsNullOrWhiteSpace(creds.AdminClientId) &&
            !string.IsNullOrWhiteSpace(creds.AdminClientSecret))
        {
            return await tokenClient.GetClientCredentialsTokenAsync(new DevControllerHelpers.ClientCredentials(
                creds.AdminClientId ?? string.Empty, creds.AdminClientSecret ?? string.Empty)).ConfigureAwait(false);
        }

        if (creds is not null && !string.IsNullOrWhiteSpace(creds.AdminUsername) &&
            !string.IsNullOrWhiteSpace(creds.AdminPassword))
        {
            return await tokenClient.GetPasswordGrantTokenAsync(new DevControllerHelpers.PasswordGrant(
                creds.AdminClientId ?? string.Empty, creds.AdminUsername ?? string.Empty,
                creds.AdminPassword ?? string.Empty)).ConfigureAwait(false);
        }

        return null;
    }
}
