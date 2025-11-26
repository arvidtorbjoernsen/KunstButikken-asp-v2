using System;
using Aspire.Hosting.ApplicationModel;

namespace KunstButikken.AppHost;

internal static partial class AppCompositionBuilder
{
    // Typed helper extracted from Composition.Configure to keep the Configure method smaller.
    public static (IResourceBuilder<IResourceWithEnvironment>, IResourceBuilder<ProjectResource>) SetupUserServiceHelper(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> usersDb,
        IResourceBuilder<IResource> keycloak,
        IResourceBuilder<IResource> postgres,
        IResourceBuilder<IResource> eventBus,
        string keycloakHttpEndpoint,
        string realmName,
        string keycloakAdminUser,
        string keycloakAdminPassword)
    {
        // Use the concrete builder references (cast to the appropriate interface) so overload resolution
        // picks the correct WithReference(...) extension method.
        var keycloakBuilder = (IResourceBuilder<IResourceWithServiceDiscovery>?)keycloak ?? throw new InvalidOperationException("keycloak builder is null or incompatible");
        var postgresBuilder = (IResourceBuilder<IResourceWithConnectionString>?)postgres ?? throw new InvalidOperationException("postgres builder is null or incompatible");
        var eventBusBuilder = (IResourceBuilder<IResourceWithServiceDiscovery>?)eventBus ?? throw new InvalidOperationException("eventBus builder is null or incompatible");

        var us = builder.AddProject("user-service", "../../KunstButikken.UserService/KunstButikken.UserService/KunstButikken.UserService.csproj")
            .WithReference(usersDb)
            .WithReference(keycloakBuilder)
            .WithReference(postgresBuilder)
            .WithReference(eventBusBuilder)
            .WithEnvironment("ConnectionStrings__Default", usersDb)
            .WithEnvironment("KEYCLOAK_ISSUER", keycloakHttpEndpoint + "/realms/" + realmName)
            .WithEnvironment("KEYCLOAK_REALM", realmName)
            .WithEnvironment("KEYCLOAK_BASE", keycloakHttpEndpoint)
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", keycloakAdminUser)
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
            .WithHttpEndpoint(57500, name: "api")
            .WithExternalHttpEndpoints()
            .WaitFor(keycloak ?? throw new InvalidOperationException("keycloak is null"))
            .WaitFor(postgres ?? throw new InvalidOperationException("postgres is null"))
            .WaitFor(eventBus ?? throw new InvalidOperationException("eventBus is null"))
            .WaitForHttp("/health");

        if (us is null) throw new InvalidOperationException("user service builder creation failed");
        return ((IResourceBuilder<IResourceWithEnvironment>)us, (IResourceBuilder<ProjectResource>)us);
    }
}
