namespace KunstButikken.AppHost;

internal static partial class AppCompositionBuilder
{
    private static (IResourceBuilder<IResourceWithEndpoints>, IResourceBuilder<IResourceWithEndpoints>) BuildFrontends(
        IDistributedApplicationBuilder builder,
        string repoRoot,
        KeycloakSettings keycloakSettings,
        string realmName,
        string keycloakClientId,
        string keycloakAngularClientId,
        string? configuredApiGateway,
        string? stripePublishableKey,
        IResourceBuilder<IResourceWithEnvironment> authGateway,
        IResourceBuilder<KeycloakResource> keycloak,
        IResourceBuilder<PostgresServerResource> postgres)
    {
        var nextFrontendPath = Path.Combine(repoRoot, "KunstButikken.Frontend");
        var angularFrontendPath = Path.Combine(repoRoot, "KunstButikken.Frontend-Ang");

        var resolvedGatewayBase = string.IsNullOrWhiteSpace(configuredApiGateway)
            ? authGateway.GetEndpointString("gateway")
            : configuredApiGateway;
        if (string.IsNullOrWhiteSpace(resolvedGatewayBase))
        {
            resolvedGatewayBase = "http://localhost:5100";
        }
        var normalizedGatewayBase = resolvedGatewayBase.TrimEnd('/');
        var auctionHubUrl = $"{normalizedGatewayBase}/hubs/auctions";

        var nextJsFrontend = builder.AddNpmApp("frontend", nextFrontendPath, "dev")
            .WithHttpEndpoint(targetPort: 3000, port: 3000, name: "web", isProxied: false)
            .WithEnvironment("NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY", stripePublishableKey ?? string.Empty)
            .WithEnvironment("NEXT_PUBLIC_API_GATEWAY", normalizedGatewayBase)
            .WithEnvironment("NEXT_PUBLIC_AUCTION_SIGNALR_URL", auctionHubUrl)
            .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_BASE_URL", keycloakSettings.BaseUrl)
            .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_REALM", realmName)
            .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_ISSUER", keycloakSettings.Issuer)
            .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_CLIENT_ID", keycloakClientId)
            .WaitFor(keycloak ?? throw new InvalidOperationException("keycloak is null"))
            .WaitFor(postgres ?? throw new InvalidOperationException("postgres is null"))
            .WaitFor(authGateway ?? throw new InvalidOperationException("authGateway is null"));

        var angularFrontend = builder.AddNpmApp("frontend-ang", angularFrontendPath)
            .WithHttpEndpoint(targetPort: 4200, port: 4200, name: "web", isProxied: false)
            .WithEnvironment("NG_APP_STRIPE_PUBLISHABLE_KEY", stripePublishableKey ?? string.Empty)
            .WithEnvironment("NG_APP_API_GATEWAY", normalizedGatewayBase)
            .WithEnvironment("NG_APP_KEYCLOAK_BASE_URL", keycloakSettings.BaseUrl)
            .WithEnvironment("NG_APP_KEYCLOAK_REALM", realmName)
            .WithEnvironment("NG_APP_KEYCLOAK_ISSUER", keycloakSettings.Issuer)
            .WithEnvironment("NG_APP_KEYCLOAK_CLIENT_ID", keycloakAngularClientId)
            .WaitFor(keycloak ?? throw new InvalidOperationException("keycloak is null"))
            .WaitFor(postgres ?? throw new InvalidOperationException("postgres is null"))
            .WaitFor(authGateway ?? throw new InvalidOperationException("authGateway is null"));

        return (nextJsFrontend, angularFrontend);
    }
}

