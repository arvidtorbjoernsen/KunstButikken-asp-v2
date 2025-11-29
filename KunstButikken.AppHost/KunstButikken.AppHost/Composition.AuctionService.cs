namespace KunstButikken.AppHost;

internal static partial class AppCompositionBuilder
{
    private static (IResourceBuilder<IResourceWithEnvironment>, IResourceBuilder<ProjectResource>) BuildAuctionService(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> auctionsDb,
        IResourceBuilder<IResourceWithEnvironment> artService,
        IResourceBuilder<IResourceWithEnvironment> userService,
        IResourceBuilder<RabbitMQServerResource> eventBus,
        IResourceBuilder<PostgresServerResource> postgres,
        KeycloakSettings keycloakSettings)
    {
        var auc = builder
            .AddProject("auction-service", "../../KunstButikken.AuctionService/KunstButikken.AuctionService/KunstButikken.AuctionService.csproj")
            .WithReference(auctionsDb)
            .WithReference(eventBus)
            .WithReference(postgres)
            .WithEnvironment("ConnectionStrings__Default", auctionsDb)
            .WithHttpEndpoint(name: "api")
            .WithExternalHttpEndpoints()
            .WaitFor(postgres ?? throw new InvalidOperationException("postgres is null"))
            .WaitFor(artService ?? throw new InvalidOperationException("artService is null"))
            .WaitFor(userService ?? throw new InvalidOperationException("userService is null"))
            .WaitFor(eventBus ?? throw new InvalidOperationException("eventBus is null"))
            .WaitForHttp("/api/health");

        auc = ApplyKeycloakEnvironment(auc, keycloakSettings);
        return (auc, auc);
    }
}

