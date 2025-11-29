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
        var stripePublishableKey = builder.Configuration["NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY"]
            ?? builder.Configuration["stripe-publishable-key"]
            ?? builder.Configuration["Stripe:PublishableKey"];
        var configuredApiGateway = builder.Configuration["NEXT_PUBLIC_API_GATEWAY"];

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
        var sharedKeycloakIssuer = builder.Configuration["KEYCLOAK_ISSUER"] ?? $"{keycloakHttpEndpoint}/realms/{realmName}";
        var sharedKeycloakBase = builder.Configuration["KEYCLOAK_BASE"] ?? keycloakHttpEndpoint;
        var sharedKeycloakAudience = builder.Configuration["KEYCLOAK_AUDIENCE"];
        var sharedKeycloakAudiences = builder.Configuration["KEYCLOAK_AUDIENCES"];
        var sharedKeycloakAuthority = builder.Configuration["KEYCLOAK_AUTHORITY"] ?? sharedKeycloakIssuer;
        var keycloakSettings = new KeycloakSettings(
            sharedKeycloakIssuer,
            sharedKeycloakBase,
            sharedKeycloakAudience,
            sharedKeycloakAudiences,
            sharedKeycloakAuthority);

        // RabbitMQ
        var eventBus = builder.AddRabbitMQ("rabbitmq");

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

        // Run the smaller setup steps using dedicated helper methods
        void SetupUserService()
        {
            (userService, userServiceLocalBuilder) = SetupUserServiceHelper(
                builder,
                usersDb,
                keycloak ?? throw new InvalidOperationException("keycloak is null"),
                eventBus ?? throw new InvalidOperationException("eventBus is null"),
                keycloakHttpEndpoint,
                realmName,
                keycloakAdminUser,
                keycloakAdminPassword,
                keycloakSettings);
        }

        void SetupArtService()
        {
            (artService, artServiceLocalBuilder) = BuildArtService(
                builder,
                artDb,
                userService ?? throw new InvalidOperationException("userService is null"),
                azurite ?? throw new InvalidOperationException("azurite is null"),
                eventBus ?? throw new InvalidOperationException("eventBus is null"),
                postgres ?? throw new InvalidOperationException("postgres is null"),
                azureBlobContainer,
                keycloakSettings);
        }

        void SetupAuctionService()
        {
            (auctionService, auctionServiceLocalBuilder) = BuildAuctionService(
                builder,
                auctionsDb,
                artService ?? throw new InvalidOperationException("artService is null"),
                userService ?? throw new InvalidOperationException("userService is null"),
                eventBus ?? throw new InvalidOperationException("eventBus is null"),
                postgres ?? throw new InvalidOperationException("postgres is null"),
                keycloakSettings);
        }

        void SetupPaymentService()
        {
            (paymentService, paymentServiceLocalBuilder) = BuildPaymentService(
                builder,
                paymentsDb,
                eventBus ?? throw new InvalidOperationException("eventBus is null"),
                postgres ?? throw new InvalidOperationException("postgres is null"),
                stripeApiKey,
                stripeWebhookSecret,
                keycloakSettings);
        }

        void SetupAdminService()
        {
            (adminService, adminServiceLocalBuilder) = BuildAdminService(
                builder,
                adminDb,
                userService ?? throw new InvalidOperationException("userService is null"),
                eventBus ?? throw new InvalidOperationException("eventBus is null"),
                postgres ?? throw new InvalidOperationException("postgres is null"),
                keycloakSettings);
        }

        void SetupAuthGateway()
        {
            authGateway = BuildAuthGateway(
                builder,
                keycloak ?? throw new InvalidOperationException("keycloak is null"),
                userServiceLocalBuilder ?? throw new InvalidOperationException("userService builder is null"),
                artServiceLocalBuilder ?? throw new InvalidOperationException("artService builder is null"),
                auctionServiceLocalBuilder ?? throw new InvalidOperationException("auctionService builder is null"),
                paymentServiceLocalBuilder ?? throw new InvalidOperationException("paymentService builder is null"),
                adminServiceLocalBuilder ?? throw new InvalidOperationException("adminService builder is null"),
                userService ?? throw new InvalidOperationException("userService is null"),
                artService ?? throw new InvalidOperationException("artService is null"),
                auctionService ?? throw new InvalidOperationException("auctionService is null"),
                paymentService ?? throw new InvalidOperationException("paymentService is null"),
                adminService ?? throw new InvalidOperationException("adminService is null"),
                postgres ?? throw new InvalidOperationException("postgres is null"),
                keycloakSettings);
        }

        void SetupFrontends()
        {
            (nextJsFrontend, angularFrontend) = BuildFrontends(
                builder,
                repoRoot,
                keycloakSettings,
                realmName,
                keycloakClientId,
                keycloakAngularClientId,
                configuredApiGateway,
                stripePublishableKey,
                authGateway ?? throw new InvalidOperationException("authGateway is null"),
                keycloak ?? throw new InvalidOperationException("keycloak is null"),
                postgres ?? throw new InvalidOperationException("postgres is null"));
        }

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
    }

    private static IResourceBuilder<T> ApplyKeycloakEnvironment<T>(IResourceBuilder<T> resource, KeycloakSettings settings)
        where T : IResourceWithEnvironment
    {
        if (!string.IsNullOrWhiteSpace(settings.Audience))
        {
            resource = resource.WithEnvironment("KEYCLOAK_AUDIENCE", settings.Audience);
        }

        if (!string.IsNullOrWhiteSpace(settings.Audiences))
        {
            resource = resource.WithEnvironment("KEYCLOAK_AUDIENCES", settings.Audiences);
        }

        if (!string.IsNullOrWhiteSpace(settings.Authority))
        {
            resource = resource.WithEnvironment("KEYCLOAK_AUTHORITY", settings.Authority);
        }

        if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            resource = resource.WithEnvironment("KEYCLOAK_BASE", settings.BaseUrl);
        }

        if (!string.IsNullOrWhiteSpace(settings.Issuer))
        {
            resource = resource.WithEnvironment("KEYCLOAK_ISSUER", settings.Issuer);
        }

        return resource;
    }

    internal record KeycloakSettings(
        string Issuer,
        string BaseUrl,
        string? Audience,
        string? Audiences,
        string? Authority);
}
