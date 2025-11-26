using System.Diagnostics.CodeAnalysis;

using Scalar.Aspire;

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

internal static partial class AppCompositionBuilder
{
    [SuppressMessage("SonarAnalyzer.CSharp", "S3776", Justification = "Large composition method; will be refactored into smaller parts incrementally.")]
    [SuppressMessage("SonarAnalyzer.CSharp", "S106", Justification = "Composition wiring - acceptable here.")]
    public static AppComposition Configure(IDistributedApplicationBuilder builder)
    {
        // Database resources (inlined so we keep strong types for downstream calls)
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

        // Azurite
        var azureBlobContainer = builder.Configuration["AzureBlob:Container"] ?? "images";
        var azurite = builder.AddContainer("azurite", "mcr.microsoft.com/azure-storage/azurite")
            .WithHttpEndpoint(targetPort: 10000, port: 10000, name: "blob")
            .WithHttpEndpoint(targetPort: 10001, port: 10001, name: "queue")
            .WithHttpEndpoint(targetPort: 10002, port: 10002, name: "table")
            .WithBindMount("../../.data/azurite", "/data");

        azurite.WaitForHttp("/", "blob");

        // Keycloak configuration (inlined to preserve types)

        var keycloakClientId = builder.Configuration["keycloak-client-id"] ?? "kunstbutikken-frontend-nextjs";
        var keycloakAngularClientId = builder.Configuration["keycloak-angular-client-id"] ?? "kunstbutikken-frontend-ang";
        const string realmName = "kunstbutikken";
        var keycloakAdminUser = builder.Configuration["keycloak-admin-user"] ?? "admin";
        var keycloakAdminPassword = builder.Configuration["keycloak-admin-password"] ?? "admin";

        var repoRoot = AppHostHelpers.FindRepoRoot();
        var keycloakRealmPath = Path.Combine(repoRoot, "tools", "keycloak", "kunstbutikken-realm.json");

        var keycloak = builder.AddKeycloak("keycloak", 8080)
            .WithRealmImport(keycloakRealmPath)
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", keycloakAdminUser)
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
            .WithBindMount("../../.data/keycloak", "/opt/keycloak/data") // optional for persistence
            .WaitForHttp($"/realms/{realmName}/.well-known/openid-configuration", "http");

        var keycloakHttpEndpoint = keycloak.GetEndpointString("http");

        // RabbitMQ
        var eventBus = builder.AddRabbitMQ("eventbus");

        // declare service variables with explicit builder interface types so extension overloads resolve
        IResourceBuilder<IResourceWithEnvironment>? userService;
        IResourceBuilder<IResourceWithEnvironment>? artService;
        IResourceBuilder<IResourceWithEnvironment>? auctionService;
        IResourceBuilder<IResourceWithEnvironment>? paymentService;
        IResourceBuilder<IResourceWithEnvironment>? adminService;
        IResourceBuilder<IResourceWithEnvironment>? authGateway;

        IResourceBuilder<IResourceWithEndpoints>? nextJsFrontend;
        IResourceBuilder<IResourceWithEndpoints>? angularFrontend;

        // Per-service concrete builder locals (used when we need concrete ProjectResource builders)
        IResourceBuilder<ProjectResource>? userServiceLocalBuilder;
        IResourceBuilder<ProjectResource>? artServiceLocalBuilder;
        IResourceBuilder<ProjectResource>? auctionServiceLocalBuilder;
        IResourceBuilder<ProjectResource>? paymentServiceLocalBuilder;
        IResourceBuilder<ProjectResource>? adminServiceLocalBuilder;

        // Run the smaller setup steps using local functions so overload resolution keeps working
        SetupUserService();
        SetupArtService();
        SetupAuctionService();
        SetupPaymentService();
        SetupAdminService();
        SetupAuthGateway();
        SetupFrontends();

        // Explicit null-checks with clear messages instead of nullable-forgiving operators.
        if (authGateway is null)
        {
            throw new InvalidOperationException("AuthGateway composition failed - authGateway is null");
        }
        if (userService is null)
        {
            throw new InvalidOperationException("UserService composition failed - userService is null");
        }
        if (artService is null)
        {
            throw new InvalidOperationException("ArtService composition failed - artService is null");
        }
        if (auctionService is null)
        {
            throw new InvalidOperationException("AuctionService composition failed - auctionService is null");
        }
        if (paymentService is null)
        {
            throw new InvalidOperationException("PaymentService composition failed - paymentService is null");
        }
        if (adminService is null)
        {
            throw new InvalidOperationException("AdminService composition failed - adminService is null");
        }
        if (nextJsFrontend is null)
        {
            throw new InvalidOperationException("NextJsFrontend composition failed - nextJsFrontend is null");
        }
        if (angularFrontend is null)
        {
            throw new InvalidOperationException("AngularFrontend composition failed - angularFrontend is null");
        }

        // Try registering Scalar API Reference centrally from AppHost so services don't need per-service mapping.
        try
        {
            // Prefer the strongly-typed API if available
            var scalarApi = builder.AddScalarApiReference(options => { options.WithTheme(ScalarTheme.Purple); });
            if (scalarApi != null)
            {
                // Register project-level API references when local project builders are available
                if (userServiceLocalBuilder != null)
                {
                    scalarApi.WithApiReference(userServiceLocalBuilder);
                }
                if (artServiceLocalBuilder != null)
                {
                    scalarApi.WithApiReference(artServiceLocalBuilder);
                }
                if (auctionServiceLocalBuilder != null)
                {
                    scalarApi.WithApiReference(auctionServiceLocalBuilder);
                }
                if (paymentServiceLocalBuilder != null)
                {
                    scalarApi.WithApiReference(paymentServiceLocalBuilder);
                }
                if (adminServiceLocalBuilder != null)
                {
                    scalarApi.WithApiReference(adminServiceLocalBuilder);
                }
                Console.WriteLine("[AppHost] Registered Scalar API references for services.");
            }
        }
        catch (Exception ex)
        {
            // If Scalar isn't available or the strongly-typed API changed, don't fail the composition — AppHost will still run.
            Console.WriteLine($"[AppHost] Scalar API registration skipped: {ex.Message}");
        }

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

        void SetupUserService() =>
            (userService, userServiceLocalBuilder) = SetupUserServiceHelper(builder, usersDb, keycloak, eventBus, keycloakHttpEndpoint, realmName, keycloakAdminUser, keycloakAdminPassword);

        void SetupArtService()
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

            if (art is null)
            {
                throw new InvalidOperationException("art service builder creation failed");
            }
            artServiceLocalBuilder = art;
            artService = art;
        }

        void SetupAuctionService()
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

            if (auc is null)
            {
                throw new InvalidOperationException("auction service builder creation failed");
            }
            auctionServiceLocalBuilder = auc;
            auctionService = auc;
        }

        void SetupPaymentService()
        {
            var pay = builder
                .AddProject("payment-service", "../../KunstButikken.PaymentService/KunstButikken.PaymentService/KunstButikken.PaymentService.csproj")
                .WithReference(paymentsDb)
                .WithReference(eventBus)
                .WithEnvironment("ConnectionStrings__Default", paymentsDb)
                .WithEnvironment("Stripe__ApiKey", stripeApiKey)
                .WithEnvironment("Stripe__WebhookSecret", stripeWebhookSecret)
                .WithHttpEndpoint(name: "api")
                .WithHttpEndpoint(name: "webhook")
                .WithExternalHttpEndpoints()
                .WaitFor(postgres ?? throw new InvalidOperationException("postgres is null"))
                .WaitFor(eventBus ?? throw new InvalidOperationException("eventBus is null"));

            if (pay is null)
            {
                throw new InvalidOperationException("payment service builder creation failed");
            }
            paymentServiceLocalBuilder = pay;
            paymentService = pay;
            paymentService.WaitForHttp("/api/health");
        }

        void SetupAdminService()
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

            if (adm is null)
            {
                throw new InvalidOperationException("admin service builder creation failed");
            }
            adminServiceLocalBuilder = adm;
            adminService = adm;
        }

        void SetupAuthGateway()
        {
            var ag = builder.AddProject("auth-gateway", "../../KunstButikken.AuthGateway/KunstButikken.AuthGateway/KunstButikken.AuthGateway.csproj")
                .WithReference((IResourceBuilder<IResourceWithServiceDiscovery>?)userServiceLocalBuilder ?? throw new InvalidOperationException("user service builder is null"))
                .WithReference((IResourceBuilder<IResourceWithServiceDiscovery>?)artServiceLocalBuilder ?? throw new InvalidOperationException("art service builder is null"))
                .WithReference((IResourceBuilder<IResourceWithServiceDiscovery>?)auctionServiceLocalBuilder ?? throw new InvalidOperationException("auction service builder is null"))
                .WithReference((IResourceBuilder<IResourceWithServiceDiscovery>?)paymentServiceLocalBuilder ?? throw new InvalidOperationException("payment service builder is null"))
                .WithReference((IResourceBuilder<IResourceWithServiceDiscovery>?)adminServiceLocalBuilder ?? throw new InvalidOperationException("admin service builder is null"))
                .WithHttpEndpoint(5100, name: "gateway")
                .WaitFor(postgres ?? throw new InvalidOperationException("postgres is null"))
                .WaitFor(userService ?? throw new InvalidOperationException("userService is null"))
                .WaitFor(artService ?? throw new InvalidOperationException("artService is null"))
                .WaitFor(auctionService ?? throw new InvalidOperationException("auctionService is null"))
                .WaitFor(paymentService ?? throw new InvalidOperationException("paymentService is null"))
                .WaitFor(adminService ?? throw new InvalidOperationException("adminService is null"))
                .WaitForHttp("/health", "gateway");

            if (ag is null)
            {
                throw new InvalidOperationException("auth gateway builder creation failed");
            }
            authGateway = ag;
        }

        void SetupFrontends()
        {
            // var repoRoot = AppHostHelpers.FindRepoRoot();
            var nextFrontendPath = Path.Combine(repoRoot, "KunstButikken.Frontend");
            var angularFrontendPath = Path.Combine(repoRoot, "KunstButikken.Frontend-Ang");

            nextJsFrontend = builder.AddNpmApp("frontend", nextFrontendPath, "dev")
                .WithHttpEndpoint(targetPort: 3000, port: 3000, name: "web", isProxied: false)
                .WithEnvironment("NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY", stripePublishableKey ?? "")
                .WithEnvironment("NEXT_PUBLIC_API_GATEWAY", authGateway.GetEndpointString("gateway"))
                .WithEnvironment("NEXT_PUBLIC_AUCTION_SIGNALR_URL", authGateway.GetEndpointString("gateway") + "/hubs/auctions")
                .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_BASE_URL", keycloakHttpEndpoint)
                .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_REALM", realmName)
                .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_ISSUER", keycloakHttpEndpoint + "/realms/" + realmName)
                .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_CLIENT_ID", keycloakClientId)
                .WaitFor(keycloak ?? throw new InvalidOperationException("keycloak is null"))
                .WaitFor(postgres ?? throw new InvalidOperationException("postgres is null"))
                .WaitFor(authGateway ?? throw new InvalidOperationException("authGateway is null"));

            angularFrontend = builder.AddNpmApp("frontend-ang", angularFrontendPath)
                .WithHttpEndpoint(targetPort: 4200, port: 4200, name: "web", isProxied: false)
                .WithEnvironment("NG_APP_STRIPE_PUBLISHABLE_KEY", stripePublishableKey ?? "")
                .WithEnvironment("NG_APP_API_GATEWAY", authGateway.GetEndpointString("gateway"))
                .WithEnvironment("NG_APP_KEYCLOAK_BASE_URL", keycloakHttpEndpoint)
                .WithEnvironment("NG_APP_KEYCLOAK_REALM", realmName)
                .WithEnvironment("NG_APP_KEYCLOAK_ISSUER", keycloakHttpEndpoint + "/realms/" + realmName)
                .WithEnvironment("NG_APP_KEYCLOAK_CLIENT_ID", keycloakAngularClientId)
                .WaitFor(keycloak ?? throw new InvalidOperationException("keycloak is null"))
                .WaitFor(postgres ?? throw new InvalidOperationException("postgres is null"))
                .WaitFor(authGateway ?? throw new InvalidOperationException("authGateway is null"));
        }
    }
}
