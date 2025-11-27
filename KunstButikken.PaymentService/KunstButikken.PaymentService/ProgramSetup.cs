using System;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using KunstButikken.Common.Logging;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.IntegrationEvents.Contracts.Events;
using KunstButikken.PaymentService.IntegrationEvents;
using KunstButikken.PaymentService.IntegrationEvents.Handlers;
using KunstButikken.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Scalar.Aspire;
using KunstButikken.PaymentService.Infrastructure.Data;
using KunstButikken.PaymentService.Infrastructure.DependencyInjection;
using KunstButikken.PaymentService.Domain.Repositories;
using KunstButikken.PaymentService.Domain.Models;
using KunstButikken.PaymentService.Application.Interfaces;
using KunstButikken.PaymentService.Application.Services;

namespace KunstButikken.PaymentService;

public static class ProgramSetup
{
    public static void ConfigureBuilder(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Toggle: set EF_STRICT_MIGRATIONS=true to log PendingModelChangesWarning instead of suppressing it
        var strictMigrations =
            string.Equals(builder.Configuration["EF_STRICT_MIGRATIONS"], "true", StringComparison.OrdinalIgnoreCase);

        // Allow binding an extra stable HTTP port via configuration (for webhooks)
        var pinnedPortStr = builder.Configuration["Payment:PinnedHttpPort"] ?? builder.Configuration["PAYMENT_PINNED_HTTP_PORT"];
        if (int.TryParse(pinnedPortStr, out var pinnedPort) && pinnedPort > 0)
        {
            builder.WebHost.ConfigureKestrel(options => { options.ListenAnyIP(pinnedPort); });
        }

        // Add common Aspire defaults (health checks, service discovery, OpenTelemetry)
        builder.AddServiceDefaults();

        // Register injectable .env loader for tests/DI
        builder.Services.AddEnvLoader();

        // Add RabbitMQ client
        builder.AddRabbitMQClient("rabbitmq");
        builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
        builder.Services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();
        builder.Services.AddScoped<AuctionEndedIntegrationEventHandler>();

        // Register example RabbitMQ publisher helper that uses IConnection injected by Aspire.RabbitMQ.Client
        builder.Services.AddSingleton<RabbitMqPublisher>();

        // Register system date/time provider
        builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Register Stripe services
        builder.Services.AddSingleton<IStripeWebhookService, StripeWebhookService>();
        builder.Services.AddSingleton<IStripeSessionService, StripeSessionService>();

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
                            OnAuthenticationFailed = context => { Console.WriteLine($"[PaymentService] Authentication failed: {context.Exception.Message}"); return Task.CompletedTask; },
                            OnTokenValidated = context => { Console.WriteLine($"[PaymentService] Token validated successfully for user: {context.Principal?.Identity?.Name}"); return Task.CompletedTask; }
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

        // Replace manual DbContext + repo registration with centralized AddInfrastructure call
        // Add EF Core DbContext
        try
        {
            builder.Services.AddInfrastructure(builder.Configuration);
        }
        catch
        {
            // Fallback: if AddInfrastructure is unavailable, keep current manual registration for robustness
            var cs = builder.Configuration.GetConnectionString("Default")
                     ?? builder.Configuration.GetConnectionString("paymentsdb")
                     ?? builder.Configuration["ConnectionStrings:Default"]
                     ?? builder.Configuration["ConnectionStrings:paymentsdb"];

            if (!string.IsNullOrWhiteSpace(cs) && !cs.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                builder.Services.AddDbContext<PaymentDbContext>(options =>
                    options.UseNpgsql(cs, npgsql => npgsql.EnableRetryOnFailure()
                        .MigrationsAssembly(typeof(PaymentDbContext).Assembly.FullName)));
            }
            else
            {
                builder.Services.AddDbContext<PaymentDbContext>(options => options.UseInMemoryDatabase("payment_inmemory_db"));
            }

            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
        }

        // Add services to the container.
        builder.Services.AddControllers();
    }

    public static async Task ConfigureApp(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Configure the HTTP request pipeline.
        var enableOpenApi = app.Environment.IsDevelopment()
                            || string.Equals(app.Configuration["ENABLE_OPENAPI"], "true",
                                StringComparison.OrdinalIgnoreCase);

        if (enableOpenApi)
        {
            TryMapScalarApiReference(app);
        }

        // Ensure DB exists with retry in Development
        using (var scope = app.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            try
            {
                var db = sp.GetRequiredService<PaymentDbContext>();
                var logger = sp.GetRequiredService<ILogger<Program>>();

                var maxRetries = 10;
                for (var i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
                        LogMessages.Information_Msg_1(logger, null);
                        break;
                    }
                    catch (Exception ex) when (i < maxRetries - 1)
                    {
                        LogMessages.Warning_Attempt_2(logger, i + 1, ex);
                        await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        LogMessages.Error_MaxRetries_3(logger, maxRetries, ex);
                        LogMessages.Warning_Msg_4(logger, null);
                        break;
                    }
                }
            }
            catch
            {
                // DbContext or logger not registered in this environment; ignore
            }
        }

        app.UseCors("frontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Map default health endpoints, etc.
        app.MapDefaultEndpoints();

        // Subscribe to integration events where applicable
        try
        {
            var eventBus = app.Services.GetRequiredService<IEventBus>();
            await eventBus.SubscribeAsync<AuctionEndedIntegrationEvent, AuctionEndedIntegrationEventHandler>().ConfigureAwait(false);
        }
        catch
        {
            // Subscription failures shouldn't prevent the app from starting
        }
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
                Console.WriteLine("[PaymentService] MapScalarApiReference extension not found (Scalar package may not expose it). Skipping Scalar API reference mapping.");
                return;
            }

            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (ps.Length == 1 && ps[0].ParameterType.IsAssignableFrom(appType))
                {
                    m.Invoke(null, new object[] { app });
                    Console.WriteLine("[PaymentService] Invoked MapScalarApiReference(WebApplication)");
                    return;
                }

                if (ps.Length == 2 && ps[0].ParameterType.IsAssignableFrom(appType))
                {
                    try
                    {
                        m.Invoke(null, new object[] { app, new NoOpScalarOptions() });
                        Console.WriteLine("[PaymentService] Invoked MapScalarApiReference(WebApplication, options) with synthetic options");
                        return;
                    }
                    catch
                    {
                        // ignore and try next
                    }
                }
            }

            Console.WriteLine("[PaymentService] No matching MapScalarApiReference overload found; skipping.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[PaymentService] Error invoking MapScalarApiReference reflectively: " + ex.Message);
        }
    }

    private sealed class NoOpScalarOptions { }
}
