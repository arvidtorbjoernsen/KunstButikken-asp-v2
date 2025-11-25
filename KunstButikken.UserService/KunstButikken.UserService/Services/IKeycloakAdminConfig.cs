namespace KunstButikken.UserService.Services;

public interface IKeycloakAdminConfig
{
  string TokenEndpoint { get; }
  string TokenRealm { get; }
  string AdminClientId { get; }
  string? AdminClientSecret { get; }
  string? AdminUsername { get; }
  string? AdminPassword { get; }

  bool HasClientCredentials { get; }
  bool HasPasswordGrant { get; }
}