using System.Diagnostics.CodeAnalysis;

namespace KunstButikken.AppHost;

internal record AppComposition(
    IResourceBuilder<IResourceWithEnvironment> AuthGateway,
    IResourceBuilder<IResourceWithEnvironment> UserService,
    IResourceBuilder<IResourceWithEnvironment> ArtService,
    IResourceBuilder<IResourceWithEnvironment> AuctionService,
    IResourceBuilder<IResourceWithEnvironment> PaymentService,
    IResourceBuilder<IResourceWithEnvironment> AdminService,
    IResourceBuilder<IResourceWithEndpoints> NextJsFrontend,
    IResourceBuilder<IResourceWithEndpoints> AngularFrontend
);

internal static class AppCompositionBuilder
{
    [SuppressMessage("SonarAnalyzer.CSharp", "S3776", Justification = "Large composition method; will be refactored into smaller parts incrementally.")]
    [SuppressMessage("SonarAnalyzer.CSharp", "S106", Justification = "Composition wiring - acceptable here.")]
    public static AppComposition Configure(IDistributedApplicationBuilder builder)
    {
        // Database resources
        var postgres = builder.AddPostgres("postgres").WithDataVolume().WithPgAdmin(c => c.WithHostPort(4711));
        var usersDb = postgres.AddDatabase("usersdb");
        var artDb = postgres.AddDatabase("artdb");
        var auctionsDb = postgres.AddDatabase("auctionsdb");
        var paymentsDb = postgres.AddDatabase("paymentsdb");
        var adminDb = postgres.AddDatabase("admindb");

        // Stripe configuration
        var stripeApiKey = builder.Configuration["stripe-api-key"] ?? builder.Configuration["Stripe:ApiKey"];
        var stripeWebhookSecret = builder.Configuration["stripe-webhook-secret"] ?? builder.Configuration["Stripe:WebhookSecret"];
        var stripePublishableKey = builder.Configuration["stripe-publishable-key"] ?? builder.Configuration["Stripe:PublishableKey"];

        // Azure Blob / Azurite configuration
        var azureBlobContainer = builder.Configuration["AzureBlob:Container"] ?? "images";
        var azurite = builder.AddContainer("azurite", "mcr.microsoft.com/azure-storage/azurite")
            .WithHttpEndpoint(targetPort: 10000, port: 10000, name: "blob")
            .WithHttpEndpoint(targetPort: 10001, port: 10001, name: "queue")
            .WithHttpEndpoint(targetPort: 10002, port: 10002, name: "table")
            .WithBindMount("../../.data/azurite", "/data");

        azurite.WaitForHttp("/", "blob");

        // Keycloak configuration
        var keycloakClientId = builder.Configuration["keycloak-client-id"] ?? "kunstbutikken-frontend-nextjs";
        var keycloakAngularClientId = builder.Configuration["keycloak-angular-client-id"] ?? "kunstbutikken-frontend-ang";
        var realmName = "kunstbutikken";
        var keycloakAdminUser = builder.Configuration["keycloak-admin-user"] ?? "admin";
        var keycloakAdminPassword = builder.Configuration["keycloak-admin-password"] ?? "admin";

        var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "latest")
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", keycloakAdminUser)
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
            .WithEnvironment("KC_DB", "dev-file")
            .WithBindMount("../../.data/keycloak", "/opt/keycloak/data")
            .WithBindMount("../../tools/keycloak", "/opt/keycloak/data/import")
            .WithArgs("start-dev", "--import-realm")
            .WithHttpEndpoint(targetPort: 8080, name: "http");

        keycloak.WaitForHttp("/realms/" + realmName + "/.well-known/openid-configuration", "http");

        var keycloakHttpEndpoint = KunstButikken.ServiceDefaults.ResourceBuilderExtensions.GetEndpointString(keycloak, "http");
        var keycloakEndpointRef = keycloak.GetEndpoint("http") ?? throw new InvalidOperationException("Keycloak http endpoint not available");

        // RabbitMQ
        var eventBus = builder.AddRabbitMQ("eventbus");

        // Microservices
        var userService = builder.AddProject("user-service", "../../KunstButikken.UserService/KunstButikken.UserService/KunstButikken.UserService.csproj")
            .WithReference(usersDb)
            .WithReference(keycloakEndpointRef)
            .WithReference(postgres)
            .WithReference(eventBus)
            .WithEnvironment("ConnectionStrings__Default", usersDb)
            .WithEnvironment("KEYCLOAK_ISSUER", keycloakHttpEndpoint + "/realms/" + realmName)
            .WithEnvironment("KEYCLOAK_REALM", realmName)
            .WithEnvironment("KEYCLOAK_BASE", keycloakHttpEndpoint)
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", keycloakAdminUser)
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
            .WithHttpEndpoint(57500, name: "api")
            .WithExternalHttpEndpoints()
            .WaitFor(keycloak)
            .WaitFor(postgres)
            .WaitFor(eventBus)
            .WaitForHttp("/health", "api");

         var azuriteBlobRef = azurite.GetEndpoint("blob") ?? throw new InvalidOperationException("Azurite blob endpoint not available");

         var artService = builder.AddProject("art-service", "../../KunstButikken.ArtService/KunstButikken.ArtService/KunstButikken.ArtService.csproj")
             .WithReference(artDb)
             .WithReference(eventBus)
             .WithReference(azuriteBlobRef)
             .WithReference(postgres)
             .WithEnvironment("ConnectionStrings__Default", artDb)
             .WithEnvironment("AzureBlob__Container", azureBlobContainer)
             .WithEnvironment("AzureBlob__PublicUrl", KunstButikken.ServiceDefaults.ResourceBuilderExtensions.GetEndpointString(azurite, "blob"))
             .WithEnvironment("AzureBlob__ConnectionString", "UseDevelopmentStorage=true")
             .WithEnvironment("USER_SERVICE_URL", KunstButikken.ServiceDefaults.ResourceBuilderExtensions.GetEndpointString(userService, "api"))
             .WithHttpEndpoint(name: "api")
             .WithExternalHttpEndpoints()
             .WaitFor(userService)
             .WaitFor(azurite)
             .WaitFor(postgres)
             .WaitFor(eventBus)
             .WaitForHttp("/api/health", "api");

        var auctionService = builder
            .AddProject("auction-service", "../../KunstButikken.AuctionService/KunstButikken.AuctionService/KunstButikken.AuctionService.csproj")
            .WithReference(auctionsDb)
            .WithReference(eventBus)
            .WithReference(postgres)
            .WithEnvironment("ConnectionStrings__Default", auctionsDb)
            .WithHttpEndpoint(name: "api")
            .WithExternalHttpEndpoints()
            .WaitFor(postgres)
            .WaitFor(artService)
            .WaitFor(userService)
            .WaitFor(eventBus)
            .WaitForHttp("/api/health", "api");

        var paymentService = builder
            .AddProject("payment-service", "../../KunstButikken.PaymentService/KunstButikken.PaymentService/KunstButikken.PaymentService.csproj")
            .WithReference(paymentsDb)
            .WithReference(eventBus)
            .WithEnvironment("ConnectionStrings__Default", paymentsDb)
            .WithEnvironment("Stripe__ApiKey", stripeApiKey)
            .WithEnvironment("Stripe__WebhookSecret", stripeWebhookSecret)
            .WithHttpEndpoint(name: "api")
            .WithHttpEndpoint(name: "webhook")
            .WithExternalHttpEndpoints()
            .WaitFor(postgres)
            .WaitFor(eventBus);

        paymentService.WaitForHttp("/api/health", "api");

        var adminService = builder
            .AddProject("admin-service", "../../KunstButikken.AdminService/KunstButikken.AdminService/KunstButikken.AdminService.csproj")
            .WithReference(adminDb)
            .WithEnvironment("ConnectionStrings__Default", adminDb)
            .WithHttpEndpoint(name: "api")
            .WithExternalHttpEndpoints()
            .WaitFor(postgres)
            .WaitFor(userService)
            .WaitFor(eventBus)
            .WaitForHttp("/api/health", "api");

        var authGateway = builder.AddProject("auth-gateway", "../../KunstButikken.AuthGateway/KunstButikken.AuthGateway/KunstButikken.AuthGateway.csproj")
            .WithReference(userService)
            .WithReference(artService)
            .WithReference(auctionService)
            .WithReference(paymentService)
            .WithReference(adminService)
            .WithHttpEndpoint(5100, name: "gateway")
            .WaitFor(postgres)
            .WaitFor(userService)
            .WaitFor(artService)
            .WaitFor(auctionService)
            .WaitFor(paymentService)
            .WaitFor(adminService)
            .WaitForHttp("/health", "gateway");

        // Next.js Frontend (use absolute paths so npm/pnpm is run in the frontend folder)
        var repoRoot = AppHostHelpers.FindRepoRoot();
        var nextFrontendPath = System.IO.Path.Combine(repoRoot, "KunstButikken.Frontend");
        var angularFrontendPath = System.IO.Path.Combine(repoRoot, "KunstButikken.Frontend-Ang");

        var nextJsFrontend = builder.AddNpmApp("frontend", nextFrontendPath, "dev")
            .WithHttpEndpoint(targetPort: 3000, port: 3000, name: "web", isProxied: false)
            .WithEnvironment("NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY", stripePublishableKey ?? "")
            .WithEnvironment("NEXT_PUBLIC_API_GATEWAY", KunstButikken.ServiceDefaults.ResourceBuilderExtensions.GetEndpointString(authGateway, "gateway"))
            .WithEnvironment("NEXT_PUBLIC_AUCTION_SIGNALR_URL", KunstButikken.ServiceDefaults.ResourceBuilderExtensions.GetEndpointString(authGateway, "gateway") + "/hubs/auctions")
            .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_BASE_URL", keycloakHttpEndpoint)
            .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_REALM", realmName)
            .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_ISSUER", keycloakHttpEndpoint + "/realms/" + realmName)
            .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_CLIENT_ID", keycloakClientId)
            .WaitFor(keycloak)
            .WaitFor(postgres)
            .WaitFor(authGateway);

        var angularFrontend = builder.AddNpmApp("frontend-ang", angularFrontendPath, "start")
            .WithHttpEndpoint(targetPort: 4200, port: 4200, name: "web", isProxied: false)
            .WithEnvironment("NG_APP_STRIPE_PUBLISHABLE_KEY", stripePublishableKey ?? "")
            .WithEnvironment("NG_APP_API_GATEWAY", KunstButikken.ServiceDefaults.ResourceBuilderExtensions.GetEndpointString(authGateway, "gateway"))
            .WithEnvironment("NG_APP_KEYCLOAK_BASE_URL", keycloakHttpEndpoint)
            .WithEnvironment("NG_APP_KEYCLOAK_REALM", realmName)
            .WithEnvironment("NG_APP_KEYCLOAK_ISSUER", keycloakHttpEndpoint + "/realms/" + realmName)
            .WithEnvironment("NG_APP_KEYCLOAK_CLIENT_ID", keycloakAngularClientId)
            .WaitFor(keycloak)
            .WaitFor(postgres)
            .WaitFor(authGateway);

        return new AppComposition(
            authGateway,
            userService,
            artService,
            auctionService,
            paymentService,
            adminService,
            nextJsFrontend,
            angularFrontend
        );
    }
}
