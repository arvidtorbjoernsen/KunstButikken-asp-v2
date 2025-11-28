using System;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.ServiceDefaults;
using KunstButikken.UserService.Data;
using KunstButikken.UserService.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Scalar.Aspire;
using KunstButikken.UserService.Infrastructure.DependencyInjection;
using KunstButikken.UserService.Services;
using KunstButikken.UserService.Application.DependencyInjection;

namespace KunstButikken.UserService;

public static class ProgramSetup
{
    public static void ConfigureBuilder(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Toggle: set EF_STRICT_MIGRATIONS=true to log PendingModelChangesWarning instead of suppressing it
        var strictMigrations =
            string.Equals(builder.Configuration["EF_STRICT_MIGRATIONS"], "true", StringComparison.OrdinalIgnoreCase);

        // Add common Aspire defaults (health checks, service discovery, OpenTelemetry)
        builder.AddServiceDefaults();

        // Add RabbitMQ client for integration events
        builder.AddRabbitMQClient("rabbitmq");
        builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
        builder.Services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

        // Register injectable .env loader for tests/DI
        builder.Services.AddEnvLoader();

        // Register system date/time provider
        builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // CORS for frontend origin(s)
        var frontendOriginsRaw = builder.Configuration["FRONTEND_ORIGINS"] ??
                                 builder.Configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";
        var frontendOrigins = frontendOriginsRaw
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (frontendOrigins.Length == 0)
        {
            frontendOrigins = new[] { "http://localhost:3000" };
        }

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("frontend", p =>
                p.WithOrigins(frontendOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddApplication();

        builder.Services.AddControllers()
            .AddJsonOptions(options => { options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase; });

        // Authentication (Keycloak via Aspire helper)
        var realm = builder.Configuration["KEYCLOAK_REALM"] ?? "kunstbutikken";
        var audience = builder.Configuration["KEYCLOAK_AUDIENCE"] ?? builder.Configuration["Authentication:Audience"] ?? "kunstbutikken-api";

        var authority = builder.Configuration["KEYCLOAK_AUTHORITY"] ?? builder.Configuration["Authentication:Authority"];

        // If Keycloak is configured (authority or realm present) use Aspire's Keycloak helper
        if (!string.IsNullOrWhiteSpace(authority) || !string.IsNullOrWhiteSpace(realm))
        {
            // Register JwtBearer using the Keycloak helper which configures authority/audience/validation
            builder.Services
                .AddAuthentication()
                .AddKeycloakJwtBearer("keycloak", realm: realm, options =>
                {
                    options.Audience = audience;
                    // Optional: Add logging for development
                    if (builder.Environment.IsDevelopment())
                    {
                        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                        {
                            OnAuthenticationFailed = context => { Console.WriteLine($"[UserService] Authentication failed: {context.Exception.Message}"); return Task.CompletedTask; },
                            OnTokenValidated = context => { Console.WriteLine($"[UserService] Token validated successfully for user: {context.Principal?.Identity?.Name}"); return Task.CompletedTask; }
                        };
                    }
                });

            builder.Services.AddAuthorization();
        }
        else
        {
            // Fallback: register authentication system without specific scheme — keep authorization enabled
            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();
        }

        builder.Services.AddHttpClient();

        builder.Services.AddSingleton<IKeycloakSeeder, KeycloakSeeder>();
        builder.Services.AddHostedService<KeycloakSeedingHostedService>();

        builder.Services.AddSingleton<KeycloakUserProcessor>();
        builder.Services.AddHttpClient<KeycloakSeederService>();
        builder.Services.AddScoped<DevSeedService>();
    }

    public static async Task ConfigureApp(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Configure the HTTP request pipeline.
        // Enable OpenAPI/Scalar in Development or when explicitly configured
        var enableOpenApi = app.Environment.IsDevelopment()
                            || string.Equals(app.Configuration["ENABLE_OPENAPI"], "true",
                                StringComparison.OrdinalIgnoreCase);

        if (enableOpenApi)
        {
            TryMapScalarApiReference(app);
        }

        // Ensure DB exists + non-destructive seed if empty
        using (var scope = app.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;

            try
            {
                var db = sp.GetRequiredService<UserDbContext>();
                try
                {
                    await db.Database.MigrateAsync().ConfigureAwait(false);
                }
                catch
                {
                    // In tests we may use in-memory database that doesn't support migrations; ignore failures here
                }
            }
            catch
{
                // DbContext not registered in this environment; ignore
            }
        }

        app.UseCors("frontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Map default health endpoints, etc.
        app.MapDefaultEndpoints();
    }

    // Reflection-based Scalar API mapper
    private static void TryMapScalarApiReference(WebApplication app)
    {
        try
        {
            var appType = app.GetType();
            var methods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetExportedTypes(); } catch { return Array.Empty<Type>(); }
                })
                .Where(t => t.IsSealed && t.IsAbstract)
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(m => string.Equals(m.Name, "MapScalarApiReference", StringComparison.Ordinal))
                .ToList();

            if (!methods.Any())
            {
                Console.WriteLine("[UserService] MapScalarApiReference extension not found (Scalar package may not expose it). Skipping Scalar API reference mapping.");
                return;
            }

            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (ps.Length == 1 && ps[0].ParameterType.IsAssignableFrom(appType))
                {
                    m.Invoke(null, new object[] { app });
                    Console.WriteLine("[UserService] Invoked MapScalarApiReference(WebApplication)");
                    return;
                }

                if (ps.Length == 2 && ps[0].ParameterType.IsAssignableFrom(appType))
                {
                    try
                    {
                        m.Invoke(null, new object[] { app, null });
                        Console.WriteLine("[UserService] Invoked MapScalarApiReference(WebApplication, options) with null options");
                        return;
                    }
                    catch
                    {
                        // ignore and try next
                    }
                }
            }

            Console.WriteLine("[UserService] No matching MapScalarApiReference overload found; skipping.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[UserService] Error invoking MapScalarApiReference reflectively: " + ex.Message);
        }
    }
}
