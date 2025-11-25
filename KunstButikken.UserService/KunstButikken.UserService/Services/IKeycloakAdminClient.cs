namespace KunstButikken.UserService.Services;

public interface IKeycloakAdminClient
{
  Task<AdminTokenResult> TryGetAdminTokenAsync(HttpClient http, string issuer, CancellationToken ct);
}