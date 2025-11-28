namespace KunstButikken.UserService.Infrastructure.Keycloak.Sync;

public interface IKeycloakAdminClient
{
    Task<(string? Token, string? TokenEndpoint, string? TokenRealm, string? ClientId, string? GrantTried, string? HttpError)> TryGetAdminTokenAsync(HttpClient client, string issuer, CancellationToken cancellationToken);
}
