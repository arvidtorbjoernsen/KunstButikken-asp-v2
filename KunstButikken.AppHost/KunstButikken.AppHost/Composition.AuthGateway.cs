namespace KunstButikken.AppHost;

internal static partial class AppCompositionBuilder
{
    private static IResourceBuilder<IResourceWithEnvironment> BuildAuthGateway(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<KeycloakResource> keycloak,
        IResourceBuilder<ProjectResource> userServiceLocalBuilder,
        IResourceBuilder<ProjectResource> artServiceLocalBuilder,
        IResourceBuilder<ProjectResource> auctionServiceLocalBuilder,
        IResourceBuilder<ProjectResource> paymentServiceLocalBuilder,
        IResourceBuilder<ProjectResource> adminServiceLocalBuilder,
        IResourceBuilder<IResourceWithEnvironment> userService,
        IResourceBuilder<IResourceWithEnvironment> artService,
        IResourceBuilder<IResourceWithEnvironment> auctionService,
        IResourceBuilder<IResourceWithEnvironment> paymentService,
        IResourceBuilder<IResourceWithEnvironment> adminService,
        IResourceBuilder<PostgresServerResource> postgres,
        KeycloakSettings keycloakSettings)
    {
        var ag = builder.AddProject("auth-gateway", "../../KunstButikken.AuthGateway/KunstButikken.AuthGateway/KunstButikken.AuthGateway.csproj")
            .WithReference((IResourceBuilder<IResourceWithServiceDiscovery>)userServiceLocalBuilder)
            .WithReference((IResourceBuilder<IResourceWithServiceDiscovery>)artServiceLocalBuilder)
            .WithReference((IResourceBuilder<IResourceWithServiceDiscovery>)auctionServiceLocalBuilder)
            .WithReference((IResourceBuilder<IResourceWithServiceDiscovery>)paymentServiceLocalBuilder)
            .WithReference((IResourceBuilder<IResourceWithServiceDiscovery>)adminServiceLocalBuilder)
            .WithReference(keycloak)
            .WithHttpEndpoint(5100, name: "gateway")
            .WaitFor(postgres ?? throw new InvalidOperationException("postgres is null"))
            .WaitFor(userService ?? throw new InvalidOperationException("userService is null"))
            .WaitFor(artService ?? throw new InvalidOperationException("artService is null"))
            .WaitFor(auctionService ?? throw new InvalidOperationException("auctionService is null"))
            .WaitFor(paymentService ?? throw new InvalidOperationException("paymentService is null"))
            .WaitFor(adminService ?? throw new InvalidOperationException("adminService is null"))
            .WaitForHttp("/health", "gateway");

        ag = ApplyKeycloakEnvironment(ag, keycloakSettings);
        return ag;
    }
}

