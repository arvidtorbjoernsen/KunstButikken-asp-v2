namespace KunstButikken.UserService.Infrastructure.Keycloak.Sync;

public interface IKeycloakAdminConfig
{
    string Issuer { get; }
    string Realm { get; }
    string AdminBase { get; }
}
