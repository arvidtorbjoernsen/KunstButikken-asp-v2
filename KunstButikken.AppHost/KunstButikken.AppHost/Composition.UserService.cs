namespace KunstButikken.AppHost;

internal static partial class AppCompositionBuilder
{
    // Typed helper extracted from Composition.Configure to keep the Configure method smaller.
    public static (IResourceBuilder<IResourceWithEnvironment>, IResourceBuilder<ProjectResource>) SetupUserServiceHelper(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> usersDb,
        IResourceBuilder<KeycloakResource> keycloak,
        IResourceBuilder<RabbitMQServerResource> eventBus,
        string keycloakHttpEndpoint,
        string realmName,
        string keycloakAdminUser,
        string keycloakAdminPassword,
        KeycloakSettings keycloakSettings)
    {
        // Use the concrete builder references (cast to the appropriate interface) so overload resolution
        // picks the correct WithReference(...) extension method.
        var keycloakBuilder = (IResourceBuilder<KeycloakResource>?)keycloak ?? throw new InvalidOperationException("keycloak builder is null or incompatible");
        var eventBusBuilder = (IResourceBuilder<RabbitMQServerResource>?)eventBus ?? throw new InvalidOperationException("eventBus builder is null or incompatible");

        var us = builder.AddProject("user-service", "../../KunstButikken.UserService/KunstButikken.UserService/KunstButikken.UserService.csproj")
            .WithReference(usersDb)
            .WithReference(keycloakBuilder)
            .WithReference(eventBusBuilder)
            .WithEnvironment("ConnectionStrings__Default", usersDb)
            .WithEnvironment("KEYCLOAK_ISSUER", keycloakHttpEndpoint)
            .WithEnvironment("KEYCLOAK_REALM", realmName)
            .WithEnvironment("KEYCLOAK_BASE", keycloakHttpEndpoint)
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", keycloakAdminUser)
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
            .WithHttpEndpoint(57500, name: "api")
            .WithExternalHttpEndpoints()
            .WaitFor(keycloak ?? throw new InvalidOperationException("keycloak is null"))
            .WaitFor(eventBus ?? throw new InvalidOperationException("eventBus is null"))
            .WaitForHttp("/health");

        us = ApplyKeycloakEnvironment(us, keycloakSettings);

        if (us is null)
        {
            throw new InvalidOperationException("user service builder creation failed");
        }
        return (us, us);
    }
}
