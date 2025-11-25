// Use the main Aspire.Hosting namespace (extension methods live in this namespace)
using Aspire.Hosting;
using KunstButikken.ServiceDefaults;
// Optional: Model Context Protocol runtime integration (loaded reflectively)
using System.Reflection;
using System;
using System.Linq;
using System.Collections.Generic;

var builder = DistributedApplication.CreateBuilder(args);

// Database resources
var postgres = builder.AddPostgres("postgres").WithDataVolume().WithPgAdmin(c => c.WithHostPort(4711));
var usersDb = postgres.AddDatabase("usersdb");
var artDb = postgres.AddDatabase("artdb");
var auctionsDb = postgres.AddDatabase("auctionsdb");
var paymentsDb = postgres.AddDatabase("paymentsdb");
var adminDb = postgres.AddDatabase("admindb");

// Stripe configuration
var stripeApiKey = builder.Configuration["stripe-api-key"] ?? builder.Configuration["Stripe:ApiKey"];
var stripeWebhookSecret =
    builder.Configuration["stripe-webhook-secret"] ?? builder.Configuration["Stripe:WebhookSecret"];
var stripePublishableKey =
    builder.Configuration["stripe-publishable-key"] ?? builder.Configuration["Stripe:PublishableKey"];

// Azure Blob / Azurite configuration
var azureBlobContainer = builder.Configuration["AzureBlob:Container"] ?? "images";
var azurite = builder.AddContainer("azurite", "mcr.microsoft.com/azure-storage/azurite")
    .WithHttpEndpoint(targetPort: 10000, port: 10000, name: "blob")
    .WithHttpEndpoint(targetPort: 10001, port: 10001, name: "queue")
    .WithHttpEndpoint(targetPort: 10002, port: 10002, name: "table")
    .WithBindMount("../../.data/azurite", "/data");

// Wait for azurite blob endpoint to be reachable before services that depend on blob storage start
azurite.WaitForHttp("/", "blob");

// Keycloak configuration
var keycloakClientId = builder.Configuration["keycloak-client-id"] ?? "kunstbutikken-frontend-nextjs";
var keycloakAngularClientId = builder.Configuration["keycloak-angular-client-id"] ?? "kunstbutikken-frontend-ang";
var realmName = "kunstbutikken";
var keycloakAdminUser = builder.Configuration["keycloak-admin-user"] ?? "admin";
var keycloakAdminPassword = builder.Configuration["keycloak-admin-password"] ?? "admin";

// Keycloak container
var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "latest")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", keycloakAdminUser)
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
    .WithEnvironment("KC_DB", "dev-file")
    .WithBindMount("../../.data/keycloak", "/opt/keycloak/data")
    .WithBindMount("../../tools/keycloak", "/opt/keycloak/data/import")
    .WithArgs("start-dev", "--import-realm")
    .WithHttpEndpoint(targetPort: 8080, name: "http");

// Wait for Keycloak OIDC discovery to be reachable (prevents frontend/keycloak-init hangs)
keycloak.WaitForHttp($"/realms/{realmName}/.well-known/openid-configuration", "http");

// RabbitMQ
var eventBus = builder.AddRabbitMQ("eventbus");

// Microservices
var userService = builder.AddProject("user-service", "../../KunstButikken.UserService/KunstButikken.UserService/KunstButikken.UserService.csproj")
    .WithReference(usersDb)
    .WithReference(keycloak.GetEndpoint("http"))
    .WithReference(postgres)
    .WithReference(eventBus)
    .WithEnvironment("ConnectionStrings__Default", usersDb)
    .WithEnvironment("KEYCLOAK_ISSUER", $"{keycloak.GetEndpoint("http")}/realms/{realmName}")
    .WithEnvironment("KEYCLOAK_REALM", realmName)
    .WithEnvironment("KEYCLOAK_BASE", keycloak.GetEndpoint("http"))
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", keycloakAdminUser)
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
    // Pin the host port for the user-service to avoid collisions with macOS system services.
    // Choose 57500 as a high ephemeral port unlikely to be in use; adjust if necessary.
    .WithHttpEndpoint(57500, name: "api")
    .WithExternalHttpEndpoints()
    .WaitFor(keycloak)
    .WaitFor(postgres)
    .WaitFor(eventBus)
    .WaitForHttp("/health", "api");

var artService = builder.AddProject("art-service", "../../KunstButikken.ArtService/KunstButikken.ArtService/KunstButikken.ArtService.csproj")
    .WithReference(artDb)
    .WithReference(eventBus)
    .WithReference(azurite.GetEndpoint("blob"))
    .WithReference(postgres)
    .WithEnvironment("ConnectionStrings__Default", artDb)
    .WithEnvironment("AzureBlob__Container", azureBlobContainer)
    .WithEnvironment("AzureBlob__PublicUrl", azurite.GetEndpoint("blob"))
    .WithEnvironment("AzureBlob__ConnectionString", "UseDevelopmentStorage=true")
    .WithEnvironment("USER_SERVICE_URL", userService.GetEndpoint("api"))
    .WithHttpEndpoint(name: "api")
    .WithExternalHttpEndpoints()
    .WaitFor(userService)
    .WaitFor(azurite)
    .WaitFor(postgres)
    .WaitFor(eventBus)
    .WaitFor(userService)
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

// Wait for payment service health
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

// API Gateway
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


// Helper to find repository root (looks for solution or directory props)
static string FindRepoRoot()
{
    var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
    int maxLevels = 12;
    for (int i = 0; i < maxLevels && dir != null; i++)
    {
        // Accept both plural and singular forms and Directory.Build.props
        if (System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "Directory.Packages.props")) ||
            System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "Directory.Package.props")) ||
            System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            return dir.FullName;
        }
        // Prefer the named outer repository folder if present
        if (string.Equals(dir.Name, "KunstButikken-asp", System.StringComparison.OrdinalIgnoreCase))
        {
            return dir.FullName;
        }
        // If we find the inner solution, use it as candidate but keep searching upward for top-level markers
        if (System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "KunstButikken.sln")))
        {
            var candidate = dir.FullName;
            var up = dir.Parent;
            int innerLevels = 0;
            while (up != null && innerLevels < maxLevels)
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(up.FullName, "Directory.Packages.props")) ||
                    System.IO.File.Exists(System.IO.Path.Combine(up.FullName, "Directory.Package.props")) ||
                    string.Equals(up.Name, "KunstButikken-asp", System.StringComparison.OrdinalIgnoreCase))
                {
                    return up.FullName;
                }
                up = up.Parent;
                innerLevels++;
            }
            return candidate;
        }
        dir = dir.Parent;
    }
    // fallback: assume repo is 2 levels up from base dir
    return System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", ".."));
}

var repoRoot = FindRepoRoot();
// debug output so you can see what the resolved paths are at runtime
Console.WriteLine($"[AppHost] Repo root resolved to: {repoRoot}");
var nextFrontendPath = System.IO.Path.Combine(repoRoot, "KunstButikken.Frontend");
var angularFrontendPath = System.IO.Path.Combine(repoRoot, "KunstButikken.Frontend-Ang");
Console.WriteLine($"[AppHost] Next frontend path: {nextFrontendPath}");
Console.WriteLine($"[AppHost] Angular frontend path: {angularFrontendPath}");

// Next: explicit MCP registration (strongly-typed) if packages are present
// We use the service collection exposed by the builder to call AddMcpServer() and configure transports.
try
{
    var servicesProp = builder.GetType().GetProperty("Services");
    if (servicesProp != null)
    {
        var services = servicesProp.GetValue(builder) as Microsoft.Extensions.DependencyInjection.IServiceCollection;
        if (services != null)
        {
            try
            {
                // This extension method is provided by the ModelContextProtocol package
                // Use positional null for the optional configuration Action to match available overloads.
                var mcpBuilder = Microsoft.Extensions.DependencyInjection.McpServerServiceCollectionExtensions.AddMcpServer(services, null);

                // Note: ASP.NET HTTP transport is wired when MapMcp is invoked on the WebApplication's
                // IEndpointRouteBuilder. There is no WithHttpTransport extension in this package surface,
                // so we skip transport configuration here and rely on MapMcp to expose HTTP endpoints.

                // Add authorization filters if available (this helper lives on HttpMcpServerBuilderExtensions)
                try
                {
                    Microsoft.Extensions.DependencyInjection.HttpMcpServerBuilderExtensions.AddAuthorizationFilters(mcpBuilder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AppHost] Warning: AddAuthorizationFilters failed: {ex.Message}");
                }

                Console.WriteLine("[AppHost] Explicitly registered MCP server via AddMcpServer.");
            }
            catch (System.MissingMethodException mmex)
            {
                Console.WriteLine($"[AppHost] Explicit AddMcpServer method not found: {mmex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppHost] Explicit MCP registration failed: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("[AppHost] builder.Services is null or not IServiceCollection; skipping explicit MCP registration.");
        }
    }
    else
    {
        Console.WriteLine("[AppHost] builder.Services property not found; skipping explicit MCP registration.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[AppHost] Error while attempting explicit MCP registration: {ex.Message}");
}

// Next.js Frontend (use absolute paths so npm/pnpm is run in the frontend folder)
var nextJsFrontend = builder.AddNpmApp("frontend", nextFrontendPath, "dev")
    .WithHttpEndpoint(targetPort: 3000, port: 3000, name: "web", isProxied: false)
    .WithEnvironment("NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY", stripePublishableKey ?? "")
    .WithEnvironment("NEXT_PUBLIC_API_GATEWAY", authGateway.GetEndpoint("gateway"))
    .WithEnvironment("NEXT_PUBLIC_AUCTION_SIGNALR_URL", $"{authGateway.GetEndpoint("gateway")}/hubs/auctions")
    .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_BASE_URL", keycloak.GetEndpoint("http"))
    .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_REALM", realmName)
    .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_ISSUER", $"{keycloak.GetEndpoint("http")}/realms/{realmName}")
    .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_CLIENT_ID", keycloakClientId)
    .WaitFor(keycloak)
    .WaitFor(postgres)
    .WaitFor(authGateway);

// Angular Frontend (run npm script 'start' in the angular frontend folder)
var angularFrontend = builder.AddNpmApp("frontend-ang", angularFrontendPath, "start")
    .WithHttpEndpoint(targetPort: 4200, port: 4200, name: "web", isProxied: false)
    .WithEnvironment("NG_APP_STRIPE_PUBLISHABLE_KEY", stripePublishableKey ?? "")
    .WithEnvironment("NG_APP_API_GATEWAY", authGateway.GetEndpoint("gateway"))
    .WithEnvironment("NG_APP_KEYCLOAK_BASE_URL", keycloak.GetEndpoint("http"))
    .WithEnvironment("NG_APP_KEYCLOAK_REALM", realmName)
    .WithEnvironment("NG_APP_KEYCLOAK_ISSUER", $"{keycloak.GetEndpoint("http")}/realms/{realmName}")
    .WithEnvironment("NG_APP_KEYCLOAK_CLIENT_ID", keycloakAngularClientId)
    .WaitFor(keycloak)
    .WaitFor(postgres)
    .WaitFor(authGateway);

// Propagate CORS
PropagateCors(
    new[] { nextJsFrontend, angularFrontend },
    new IResourceBuilder<IResourceWithEnvironment>[] { authGateway, userService, artService, auctionService, paymentService, adminService });

// Attempt to configure ModelContextProtocol Server if the package/assembly is available
try
{
    // First, try targeted known extension types/methods to make invoking deterministic
    var knownAssemblies = new[] { "ModelContextProtocol.AspNetCore", "ModelContextProtocol.Core", "ModelContextProtocol" };
    var knownTypesAndMethods = new[] {
        new { TypeName = "ModelContextProtocol.AspNetCore.ServiceCollectionExtensions", Method = "AddModelContextProtocolServer" },
        new { TypeName = "ModelContextProtocol.AspNetCore.ServiceCollectionExtensions", Method = "AddModelContextProtocol" },
        new { TypeName = "ModelContextProtocol.AspNetCore.ModelContextProtocolExtensions", Method = "AddModelContextProtocolServer" },
        new { TypeName = "ModelContextProtocol.AspNetCore.ModelContextProtocolExtensions", Method = "AddModelContextProtocol" },
        new { TypeName = "ModelContextProtocol.ServiceCollectionExtensions", Method = "AddModelContextProtocol" },
        new { TypeName = "ModelContextProtocol.ServiceCollectionExtensions", Method = "AddModelContextProtocolServer" }
    };

    bool configured = false;
    foreach (var asmName in knownAssemblies)
    {
        try
        {
            var asm = Assembly.Load(new AssemblyName(asmName));
            if (asm == null) continue;
            foreach (var kv in knownTypesAndMethods)
            {
                try
                {
                    var t = asm.GetType(kv.TypeName, throwOnError: false, ignoreCase: true);
                    if (t == null) continue;
                    var m = t.GetMethod(kv.Method, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                    if (m == null) continue;
                    Console.WriteLine($"[AppHost] Invoking targeted MCP extension {kv.TypeName}.{kv.Method} from {asmName}");
                    var ps = m.GetParameters();
                    if (ps.Length == 2 && ps[0].ParameterType.Name.IndexOf("IServiceCollection", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var servicesProp = builder.GetType().GetProperty("Services");
                        if (servicesProp != null)
                        {
                            var services = servicesProp.GetValue(builder);
                            // Many AddMcpServer overloads take an Action<McpServerOptions> as the second argument.
                            // Passing builder.Configuration (ConfigurationManager) caused an invalid cast earlier.
                            // Safer: pass null for options when invoking reflectively, the implementation should handle null.
                            try
                            {
                                m.Invoke(null, new object[] { services, null });
                                configured = true;
                                break;
                            }
                            catch (TargetInvocationException tie)
                            {
                                Console.WriteLine($"[AppHost] Reflection AddMcpServer invocation threw: {tie.InnerException?.Message ?? tie.Message}");
                                // continue trying other candidates
                            }
                        }
                    }
                    if (ps.Length == 2 && ps[0].ParameterType.Name.IndexOf("DistributedApplication", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        m.Invoke(null, new object[] { builder, builder.Configuration });
                        configured = true; break;
                    }
                    if (ps.Length == 1 && ps[0].ParameterType.Name.IndexOf("DistributedApplication", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        m.Invoke(null, new object[] { builder });
                        configured = true; break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AppHost] Targeted MCP invocation failed for {kv.TypeName}.{kv.Method}: {ex.Message}");
                }
            }
            if (configured) break;
        }
        catch (Exception) { /* ignore assembly load failures here */ }
    }

    if (!configured)
    {
        // Fallback: generic probe through assemblies and methods
        var candidateNames = new[] { "ModelContextProtocol.AspNetCore", "ModelContextProtocol", "ModelContextProtocol.Core", "ModelContextProtocol.Server", "ModelContextProtocol.AspNetCore.Server" };
        foreach (var name in candidateNames)
        {
            try
            {
                var asm = Assembly.Load(new AssemblyName(name));
                if (asm == null)
                    continue;
                Console.WriteLine($"[AppHost] Found assembly: {name}");

                var types = asm.GetExportedTypes();
                // Look for any static helper class with public static methods
                foreach (var t in types)
                {
                    if (!t.IsSealed || !t.IsAbstract) // static class
                        continue;
                    var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static);
                    foreach (var m in methods)
                    {
                        // Heuristics: method name contains Configure, Add, Use or Map and mentions ModelContext or MCP
                        var mn = m.Name ?? string.Empty;
                        if (!(mn.IndexOf("Configure", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              mn.IndexOf("Add", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              mn.IndexOf("Use", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              mn.IndexOf("Map", StringComparison.OrdinalIgnoreCase) >= 0))
                            continue;

                        var sig = string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name));
                        Console.WriteLine($"[AppHost] Candidate MCP method: {t.FullName}.{m.Name}({sig})");

                        // Try to invoke common signatures safely
                        var ps = m.GetParameters();
                        try
                        {
                            if (ps.Length == 2 && ps[0].ParameterType.Name.IndexOf("DistributedApplication", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                m.Invoke(null, new object[] { builder, builder.Configuration });
                                Console.WriteLine($"[AppHost] Invoked {m.Name} on {name} (DistributedApplication signature)");
                                configured = true;
                                break;
                            }
                            if (ps.Length == 2 && ps[0].ParameterType.Name.IndexOf("IServiceCollection", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                // try to get IServiceCollection from builder if available via property Services
                                var servicesProp = builder.GetType().GetProperty("Services");
                                if (servicesProp != null)
                                {
                                    var services = servicesProp.GetValue(builder);
                                    m.Invoke(null, new object[] { services, builder.Configuration });
                                    Console.WriteLine($"[AppHost] Invoked {m.Name} on {name} (IServiceCollection signature)");
                                    configured = true;
                                    break;
                                }
                            }
                            if (ps.Length == 1 && ps[0].ParameterType.Name.IndexOf("DistributedApplication", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                m.Invoke(null, new object[] { builder });
                                Console.WriteLine($"[AppHost] Invoked {m.Name} on {name} (single DistributedApplication param)");
                                configured = true;
                                break;
                            }
                        }
                        catch (Exception invokeEx)
                        {
                            Console.WriteLine($"[AppHost] Invocation of {m.Name} failed: {invokeEx.Message}");
                            // continue trying other methods
                        }
                    }
                    if (configured) break;
                }
                if (configured) break;
            }
            catch (System.IO.FileNotFoundException)
            {
                // assembly not present — continue
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppHost] Error while probing assembly {name}: {ex.Message}");
            }
        }
        if (!configured)
        {
            Console.WriteLine("[AppHost] No MCP configuration method invoked. If you need MCP support add the appropriate ModelContextProtocol packages to the AppHost project.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[AppHost] MCP discovery failed: {ex.Message}");
}

// Build the application and try to map MCP endpoints if the AspNetCore integration is present.
var app = builder.Build();
try
{
    // Note: Mapping MCP endpoints with MapMcp requires an IEndpointRouteBuilder (typically a WebApplication instance).
    // The Aspire DistributedApplication instance is not an IEndpointRouteBuilder, so attempting to call MapMcp here
    // will fail (we observed that at runtime). Consumers should map MCP endpoints inside their ASP.NET application
    // startup (e.g., in the Next.js/Angular project's WebApplication configuration) by calling app.MapMcp("/mcp").
    // We keep the service registration here (AddMcpServer + WithHttpTransport) so the server-side services are registered.
    Console.WriteLine("[AppHost] Note: MapMcp must be invoked on the ASP.NET application's IEndpointRouteBuilder (WebApplication). Skipping MapMcp invocation from AppHost.");
}
catch (AggregateException aex)
{
    Console.WriteLine($"[AppHost] Host failed to start: {aex.InnerException?.Message ?? aex.Message}");
    throw;
}

// Finally run the application
try
{
    app.Run();
}
catch (AggregateException aex)
{
    Console.WriteLine($"[AppHost] Host failed to start: {aex.InnerException?.Message ?? aex.Message}");
    throw;
}

// Helpers
static void PropagateCors(
    IEnumerable<IResourceBuilder<IResourceWithEndpoints>> frontends,
    IEnumerable<IResourceBuilder<IResourceWithEnvironment>> services)
{
    var origins = frontends
        .Select(f => f.GetEndpoint("web")?.ToString())
        .Where(s => !string.IsNullOrEmpty(s))
        .Distinct()
        .ToArray();
    foreach (var svc in services)
    {
        svc.WithEnvironment("FRONTEND_ORIGINS", string.Join(",", origins));
    }
}
