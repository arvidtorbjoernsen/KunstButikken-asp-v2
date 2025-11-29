using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aspire.Hosting.Azure;
using KunstButikken.ArtService.Application.DependencyInjection;
using KunstButikken.ArtService.Infrastructure.DependencyInjection;
using KunstButikken.ArtService.IntegrationEvents;
using KunstButikken.ServiceDefaults;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Scalar.Aspire;
using Microsoft.IdentityModel.Tokens;

namespace KunstButikken.ArtService;

public static class ProgramSetup
{
    public static void ConfigureBuilder(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Reduce EF Core command noise in development: only warnings and above
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

        // Add common Aspire defaults (health checks, service discovery, OpenTelemetry)
        builder.AddServiceDefaults();

        // Register injectable .env loader for tests/DI
        builder.Services.AddEnvLoader();

        // Add RabbitMQ client that connects to the resource declared by AppHost
        builder.AddRabbitMQClient("rabbitmq");
        builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
        builder.Services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

        // Register system date/time provider
        builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Register IHttpClientFactory for managed HttpClient creation
        builder.Services.AddHttpClient();

        // Authentication (Keycloak via Aspire helper)
        var realm = builder.Configuration["KEYCLOAK_REALM"] ?? "kunstbutikken";
        var fallbackAudience = builder.Configuration["KEYCLOAK_AUDIENCE"] ?? builder.Configuration["Authentication:Audience"];
        var audiences = ParseAudiences(builder.Configuration["KEYCLOAK_AUDIENCES"], fallbackAudience ?? "kunstbutikken-api");
        var authority = builder.Configuration["KEYCLOAK_AUTHORITY"] ?? builder.Configuration["Authentication:Authority"];

        if (!string.IsNullOrWhiteSpace(authority) || !string.IsNullOrWhiteSpace(realm))
        {
            builder.Services
                .AddAuthentication()
                .AddKeycloakJwtBearer("keycloak", realm: realm, options =>
                {
                    if (audiences.Count > 0)
                    {
                        options.Audience = audiences[0];
                        if (audiences.Count > 1)
                        {
                            options.TokenValidationParameters ??= new TokenValidationParameters();
                            options.TokenValidationParameters.ValidAudiences = audiences;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(authority))
                    {
                        options.Authority = authority;
                    }
                    if (builder.Environment.IsDevelopment())
                    {
                        options.RequireHttpsMetadata = false;
                    }
                    if (builder.Environment.IsDevelopment())
                    {
                        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                        {
                            OnAuthenticationFailed = context => { Console.WriteLine($"[ArtService] Authentication failed: {context.Exception.Message}"); return Task.CompletedTask; },
                            OnTokenValidated = context => { Console.WriteLine($"[ArtService] Token validated successfully for user: {context.Principal?.Identity?.Name}"); return Task.CompletedTask; }
                        };
                    }
                });
            builder.Services.AddAuthorization();
        }
        else
        {
            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();
        }

        // CORS for frontend origin(s)
        var frontendOriginsRaw = builder.Configuration["FRONTEND_ORIGINS"] ?? builder.Configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";
        var frontendOrigins = frontendOriginsRaw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

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

        // Add Infrastructure and Application layers
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddApplication();

        builder.Services.AddControllers();
    }

    public static Task ConfigureApp(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var enableOpenApi = app.Environment.IsDevelopment() || string.Equals(app.Configuration["ENABLE_OPENAPI"], "true", StringComparison.OrdinalIgnoreCase);
        if (enableOpenApi)
        {
            TryMapScalarApiReference(app);
        }

        app.UseCors("frontend");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapDefaultEndpoints();

        return Task.CompletedTask;
    }

    private static void TryMapScalarApiReference(WebApplication app)
    {
        try
        {
            var appType = app.GetType();
            var methods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetExportedTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => t.IsSealed && t.IsAbstract)
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(m => string.Equals(m.Name, "MapScalarApiReference", StringComparison.Ordinal))
                .ToList();

            if (!methods.Any())
            {
                Console.WriteLine("[ArtService] MapScalarApiReference extension not found. Skipping.");
                return;
            }

            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (ps.Length == 1 && ps[0].ParameterType.IsAssignableFrom(appType))
                {
                    m.Invoke(null, new object[] { app });
                    Console.WriteLine("[ArtService] Invoked MapScalarApiReference(WebApplication)");
                    return;
                }
                if (ps.Length == 2 && ps[0].ParameterType.IsAssignableFrom(appType))
                {
                    try
                    {
                        m.Invoke(null, new object[] { app, new NoOpScalarOptions() });
                        Console.WriteLine("[ArtService] Invoked MapScalarApiReference(WebApplication, options) with synthetic options");
                        return;
                    }
                    catch
                    {
                        // ignore and try next
                    }
                }
            }
            Console.WriteLine("[ArtService] No matching MapScalarApiReference overload found.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArtService] Error invoking MapScalarApiReference: {ex.Message}");
        }
    }

    private sealed class NoOpScalarOptions { }

    private static IReadOnlyList<string> ParseAudiences(string? rawAudiences, string defaultAudience)
    {
        var audiences = rawAudiences?.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        if (audiences.Count == 0 && !string.IsNullOrWhiteSpace(defaultAudience))
        {
            audiences.Add(defaultAudience);
        }

        return audiences;
    }
}
