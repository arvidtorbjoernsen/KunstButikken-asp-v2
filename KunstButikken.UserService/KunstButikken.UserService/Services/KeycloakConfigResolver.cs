using KunstButikken.UserService.Controllers;

namespace KunstButikken.UserService.Services;

internal static class KeycloakConfigResolver
{
    public static DevControllerHelpers.KeycloakAdminConfig? Resolve(IConfiguration cfg, SeedUsersRequest? req)
    {
        var issuer = (cfg["KEYCLOAK_ISSUER"] ?? cfg["KEYCLOAK_AUTHORITY"] ?? string.Empty).TrimEnd('/');
        var realm = req?.Realm ?? DevControllerHelpers.ResolveRealmFromIssuer(issuer) ??
            cfg["KEYCLOAK_REALM"] ?? "kunstbutikken";

        var adminBase = DevControllerHelpers.ResolveAdminBase(issuer, realm) ??
                        issuer.Replace($"/realms/{realm}", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(adminBase))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(issuer) && issuer.Contains("aspire.hosting.applicationmodel.endpointreference",
                StringComparison.OrdinalIgnoreCase))
        {
            var discoverableBase = cfg["KEYCLOAK_BASE"];
            adminBase = !string.IsNullOrWhiteSpace(discoverableBase)
                ? discoverableBase.TrimEnd('/')
                : "http://keycloak";
        }

        if (!DevControllerHelpers.HasScheme(adminBase))
        {
            adminBase = "http://" + adminBase.TrimStart('/');
        }

        var tokenRealmCfg = cfg["KEYCLOAK_ADMIN_TOKEN_REALM"];
        var tokenRealm = string.IsNullOrWhiteSpace(tokenRealmCfg) ? "master" : tokenRealmCfg;
        var tokenEndpoint =
            DevControllerHelpers.Combine(adminBase, $"/realms/{tokenRealm}/protocol/openid-connect/token");

        var adminClientId = cfg["KEYCLOAK_ADMIN_CLIENT_ID"] ?? string.Empty;
        var adminClientSecret = cfg["KEYCLOAK_ADMIN_CLIENT_SECRET"] ?? string.Empty;
        var adminUsername = cfg["KC_BOOTSTRAP_ADMIN_USERNAME"] ?? string.Empty;
        var adminPassword = cfg["KC_BOOTSTRAP_ADMIN_PASSWORD"] ?? string.Empty;

        return new DevControllerHelpers.KeycloakAdminConfig(
            adminBase,
            tokenRealm,
            new Uri(tokenEndpoint),
            new DevControllerHelpers.KeycloakCredentials(adminClientId, adminClientSecret, adminUsername,
                adminPassword),
            realm);
    }
}
