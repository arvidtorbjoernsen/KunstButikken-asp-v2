using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using KunstButikken.AuctionService.Application.DependencyInjection;
using KunstButikken.AuctionService.Hubs;
using KunstButikken.AuctionService.Infrastructure.DependencyInjection;
using KunstButikken.AuctionService.Infrastructure.Persistence;
using KunstButikken.AuctionService.IntegrationEvents;
using KunstButikken.AuctionService.IntegrationEvents.Handlers;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.IntegrationEvents.Contracts.Events;
using KunstButikken.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Scalar.Aspire;

namespace KunstButikken.AuctionService;

public static class ProgramSetup
{
    public static void ConfigureBuilder(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddServiceDefaults();
        builder.Services.AddEnvLoader();

        builder.AddRabbitMQClient("rabbitmq");
        builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
        builder.Services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();
        builder.Services.AddScoped<ArtCreatedIntegrationEventHandler>();
        builder.Services.AddScoped<ArtDeletedIntegrationEventHandler>();
        builder.Services.AddScoped<ArtUpdatedIntegrationEventHandler>();

        builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        var realm = builder.Configuration["KEYCLOAK_REALM"] ?? "kunstbutikken";
        var audience = builder.Configuration["KEYCLOAK_AUDIENCE"] ?? builder.Configuration["Authentication:Audience"] ?? "kunstbutikken-api";
        var authority = builder.Configuration["KEYCLOAK_AUTHORITY"] ?? builder.Configuration["Authentication:Authority"];

        if (!string.IsNullOrWhiteSpace(authority) || !string.IsNullOrWhiteSpace(realm))
        {
            builder.Services.AddAuthentication().AddKeycloakJwtBearer("keycloak", realm, options =>
            {
                options.Audience = audience;
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
                        OnAuthenticationFailed = context => { Console.WriteLine($"[AuctionService] Authentication failed: {context.Exception.Message}"); return Task.CompletedTask; },
                        OnTokenValidated = context => { Console.WriteLine($"[AuctionService] Token validated for user: {context.Principal?.Identity?.Name}"); return Task.CompletedTask; }
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

        var frontendOriginsRaw = builder.Configuration["FRONTEND_ORIGINS"] ?? builder.Configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";
        var frontendOrigins = frontendOriginsRaw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (frontendOrigins.Length == 0) frontendOrigins = new[] { "http://localhost:3000" };

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("frontend", p => p.WithOrigins(frontendOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
        });

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddApplication();
        builder.Services.AddControllers();
        builder.Services.AddHttpClient();
        builder.Services.AddSignalR(options => { options.EnableDetailedErrors = true; });
    }

    public static async Task ConfigureApp(WebApplication app)
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
        app.MapHub<AuctionHub>("/hubs/auctions");
        app.MapDefaultEndpoints();

        var eventBus = app.Services.GetRequiredService<IEventBus>();
        await eventBus.SubscribeAsync<ArtCreatedIntegrationEvent, ArtCreatedIntegrationEventHandler>().ConfigureAwait(false);
        await eventBus.SubscribeAsync<ArtDeletedIntegrationEvent, ArtDeletedIntegrationEventHandler>().ConfigureAwait(false);
        await eventBus.SubscribeAsync<ArtUpdatedIntegrationEvent, ArtUpdatedIntegrationEventHandler>().ConfigureAwait(false);
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
                Console.WriteLine("[AuctionService] MapScalarApiReference extension not found. Skipping.");
                return;
            }

            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (ps.Length == 1 && ps[0].ParameterType.IsAssignableFrom(appType))
                {
                    m.Invoke(null, new object[] { app });
                    Console.WriteLine("[AuctionService] Invoked MapScalarApiReference(WebApplication)");
                    return;
                }
                if (ps.Length == 2 && ps[0].ParameterType.IsAssignableFrom(appType))
                {
                    try
                    {
                        m.Invoke(null, new object[] { app, new NoOpScalarOptions() });
                        Console.WriteLine("[AuctionService] Invoked MapScalarApiReference(WebApplication, options) with synthetic options");
                        return;
                    }
                    catch { /* ignore */ }
                }
            }
            Console.WriteLine("[AuctionService] No matching MapScalarApiReference overload found.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuctionService] Error invoking MapScalarApiReference: {ex.Message}");
        }
    }

    private sealed class NoOpScalarOptions { }
}
