// ...existing code...
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json;
using KunstButikken.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.ServiceDiscovery;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Forwarder;
using Scalar.Aspire;
using Aspire.Keycloak.Authentication;

namespace KunstButikken.AuthGateway;

public static class ProgramSetup
{
    public static void ConfigureBuilder(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Add common Aspire defaults (health checks, service discovery, OpenTelemetry)
        builder.AddServiceDefaults();

        // Register injectable .env loader for tests/DI
        builder.Services.AddEnvLoader();

        // Configure service discovery to allow both HTTP and HTTPS
        builder.Services.Configure<ServiceDiscoveryOptions>(options => { options.AllowedSchemes = new[] { "http", "https" }; });

        // CORS for frontend origin(s)
        var frontendOriginsRaw = builder.Configuration["FRONTEND_ORIGINS"] ?? builder.Configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";
        var frontendOrigins = frontendOriginsRaw
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (builder.Environment.IsDevelopment())
        {
            if (!frontendOrigins.Contains("http://localhost:4200")) frontendOrigins.Add("http://localhost:4200");
            if (!frontendOrigins.Contains("http://localhost:3000")) frontendOrigins.Add("http://localhost:3000");
        }

        if (frontendOrigins.Count == 0) frontendOrigins.Add("http://localhost:3000");

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("frontend", p => p.WithOrigins(frontendOrigins.ToArray()).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
        });

        // Add YARP reverse proxy with service discovery support
        builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        // Register custom forwarder HTTP client factory that uses service discovery
        builder.Services.AddSingleton<IForwarderHttpClientFactory, ServiceDiscoveryForwarderHttpClientFactory>();

        // Pre-configure named HTTP clients for each YARP cluster with service discovery
        var clusterIds = new[]
        {
            "admin-service-cluster", "art-service-cluster", "auction-service-cluster", "payment-service-cluster",
            "user-service-cluster"
        };

        foreach (var clusterId in clusterIds)
        {
            builder.Services.AddHttpClient($"Yarp.Forwarder.{clusterId}")
                .AddServiceDiscovery()
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.None,
                    ActivityHeadersPropagator = null,
                    ConnectTimeout = TimeSpan.FromSeconds(15)
                });
        }

        // Controllers
        builder.Services.AddControllers().AddJsonOptions(options => { options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; });

        // Authentication (Keycloak via Aspire helper)
        var realm = builder.Configuration["KEYCLOAK_REALM"] ?? "kunstbutikken";
        var audience = builder.Configuration["KEYCLOAK_AUDIENCE"] ?? builder.Configuration["Authentication:Audience"] ?? "kunstbutikken-api";

        try
        {
            builder.Services
                .AddAuthentication()
                .AddKeycloakJwtBearer("keycloak", realm: realm, options =>
                {
                    options.Audience = audience;
                    // Additional token validation customization can be applied here via options.TokenValidationParameters
                });

            builder.Services.AddAuthorization();
            Console.WriteLine("[AuthGateway] Configured Keycloak JWT Bearer via Aspire helper.");
        }
        catch (Exception ex)
        {
            // If the Aspire helper isn't available for some reason, fall back to manual JwtBearer setup
            Console.WriteLine("[AuthGateway] Aspire Keycloak helper unavailable, falling back to JwtBearer: " + ex.Message);

            var authority = builder.Configuration["KEYCLOAK_AUTHORITY"] ?? builder.Configuration["Authentication:Authority"];
            var aud = builder.Configuration["KEYCLOAK_AUDIENCE"] ?? builder.Configuration["Authentication:Audience"] ?? builder.Configuration["Authentication:ClientId"];

            if (!string.IsNullOrWhiteSpace(authority) && !string.IsNullOrWhiteSpace(aud))
            {
                builder.Services
                    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.Authority = authority.TrimEnd('/');
                        options.Audience = aud;
                        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ClockSkew = TimeSpan.FromMinutes(2),
                            RoleClaimType = "roles"
                        };

                        options.Events = new JwtBearerEvents
                        {
                            OnAuthenticationFailed = context => { if (builder.Environment.IsDevelopment()) Console.WriteLine($"[AuthGateway] Authentication failed: {context.Exception.Message}"); return Task.CompletedTask; },
                            OnTokenValidated = context => { if (builder.Environment.IsDevelopment()) Console.WriteLine($"[AuthGateway] Token validated successfully for user: {context.Principal?.Identity?.Name}"); return Task.CompletedTask; },
                            OnChallenge = context => { if (builder.Environment.IsDevelopment()) Console.WriteLine($"[AuthGateway] Authentication challenge: {context.Error}, {context.ErrorDescription}"); return Task.CompletedTask; }
                        };
                    });

                builder.Services.AddAuthorization();
            }
            else
            {
                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                });
                builder.Services.AddAuthorization();
            }
        }

    }

    public static async Task ConfigureApp(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Development OpenAPI UI
        if (app.Environment.IsDevelopment())
        {
            // app.MapOpenApi(); // Removed
        }

        // Disable HTTPS redirection in Development (supports Aspire and localhost), enable otherwise
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // Add routing middleware before CORS for proper CORS evaluation
        app.UseRouting();

        // Add logging for Origin header
        app.Use(async (context, next) =>
        {
            var origin = context.Request.Headers["Origin"].ToString();
            if (!string.IsNullOrEmpty(origin)) Console.WriteLine($"[AuthGateway][CORS Debug] Incoming Request Origin: {origin}");
            await next().ConfigureAwait(false);
        });

        app.UseCors("frontend");

        // Enable WebSockets for SignalR hub forwarding via YARP
        app.UseWebSockets();

        // Add logging middleware to debug authorization header forwarding (dev only)
        if (app.Environment.IsDevelopment())
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/api/profile", StringComparison.Ordinal))
                {
                    var hasAuth = context.Request.Headers.TryGetValue("Authorization", out var authHeader);
                    Console.WriteLine($"[AuthGateway][Debug] Request to {context.Request.Path}, Authorization header present: {hasAuth}");
                    if (hasAuth)
                    {
                        var authValue = authHeader.ToString();
                        var tokenPreview = authValue.Length > 50 ? string.Concat(authValue.AsSpan(0, 50), "...") : authValue;
                        Console.WriteLine($"[AuthGateway][Debug] Authorization: {tokenPreview}");
                    }
                }

                await next().ConfigureAwait(false);
            });
        }

        if (app.Environment.IsDevelopment())
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/hubs/auctions", StringComparison.Ordinal))
                {
                    Console.WriteLine($"[AuthGateway][Hub] Incoming {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
                    var origin = context.Request.Headers["Origin"].ToString();
                    if (!string.IsNullOrEmpty(origin)) Console.WriteLine($"[AuthGateway][Hub] Origin: {origin}");
                }

                await next().ConfigureAwait(false);
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Map YARP reverse proxy endpoints
        app.MapReverseProxy();

        // Map default health endpoints, etc.
        app.MapDefaultEndpoints();

        await Task.CompletedTask;
    }
}
// ...existing code...
