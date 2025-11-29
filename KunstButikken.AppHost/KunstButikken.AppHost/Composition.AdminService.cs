namespace KunstButikken.AppHost;

internal static partial class AppCompositionBuilder
{
    private static (IResourceBuilder<IResourceWithEnvironment>, IResourceBuilder<ProjectResource>) BuildAdminService(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> adminDb,
        IResourceBuilder<IResourceWithEnvironment> userService,
        IResourceBuilder<RabbitMQServerResource> eventBus,
        IResourceBuilder<PostgresServerResource> postgres,
        KeycloakSettings keycloakSettings)
    {
        var adm = builder
            .AddProject("admin-service", "../../KunstButikken.AdminService/KunstButikken.AdminService/KunstButikken.AdminService.csproj")
            .WithReference(adminDb)
            .WithEnvironment("ConnectionStrings__Default", adminDb)
            .WithHttpEndpoint(name: "api")
            .WithExternalHttpEndpoints()
            .WaitFor(postgres ?? throw new InvalidOperationException("postgres is null"))
            .WaitFor(userService ?? throw new InvalidOperationException("userService is null"))
            .WaitFor(eventBus ?? throw new InvalidOperationException("eventBus is null"))
            .WaitForHttp("/api/health");

        adm = ApplyKeycloakEnvironment(adm, keycloakSettings);
        return (adm, adm);
    }
}

