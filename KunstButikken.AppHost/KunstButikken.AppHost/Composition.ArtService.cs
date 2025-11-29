using System;
using Aspire.Hosting.ApplicationModel;

namespace KunstButikken.AppHost;

public static partial class AppCompositionBuilder
{
    private static (IResourceBuilder<IResourceWithEnvironment>, IResourceBuilder<ProjectResource>) BuildArtService(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> artDb,
        IResourceBuilder<IResourceWithEnvironment> userService,
        IResourceBuilder<ContainerResource> azurite,
        IResourceBuilder<RabbitMQServerResource> eventBus,
        IResourceBuilder<PostgresServerResource> postgres,
        string azureBlobContainer,
        KeycloakSettings keycloakSettings)
    {
        var azRef = azurite.GetEndpoint("blob") ?? throw new InvalidOperationException("Azurite blob endpoint not available");
        var art = builder.AddProject("art-service", "../../KunstButikken.ArtService/KunstButikken.ArtService/KunstButikken.ArtService.csproj")
            .WithReference(artDb)
            .WithReference(eventBus)
            .WithReference(azRef)
            .WithReference(postgres)
            .WithEnvironment("ConnectionStrings__Default", artDb)
            .WithEnvironment("AzureBlob__Container", azureBlobContainer)
            .WithEnvironment("AzureBlob__PublicUrl", azurite.GetEndpointString("blob"))
            .WithEnvironment("AzureBlob__ConnectionString", "UseDevelopmentStorage=true")
            .WithEnvironment("USER_SERVICE_URL", userService.GetEndpointString("api"))
            .WithHttpEndpoint(name: "api")
            .WithExternalHttpEndpoints()
            .WaitFor(userService ?? throw new InvalidOperationException("userService is null"))
            .WaitFor(azurite ?? throw new InvalidOperationException("azurite is null"))
            .WaitFor(postgres ?? throw new InvalidOperationException("postgres is null"))
            .WaitFor(eventBus ?? throw new InvalidOperationException("eventBus is null"))
            .WaitForHttp("/api/health");

        art = ApplyKeycloakEnvironment(art, keycloakSettings);
        return (art, art);
    }
}
