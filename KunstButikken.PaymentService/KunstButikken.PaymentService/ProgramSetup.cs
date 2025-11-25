// ...existing code...
using System;
using System.Threading.Tasks;
using KunstButikken.Common.Logging;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.IntegrationEvents.Contracts.Events;
using KunstButikken.PaymentService.Data;
using KunstButikken.PaymentService.IntegrationEvents;
using KunstButikken.PaymentService.IntegrationEvents.Handlers;
using KunstButikken.PaymentService.Services;
using KunstButikken.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;
using KunstButikken.PaymentService.Infrastructure.DependencyInjection;

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
        builder.AddRabbitMQClient("eventbus");
        builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
        builder.Services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();
        builder.Services.AddScoped<AuctionEndedIntegrationEventHandler>();

        // Register system date/time provider
        builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Register Stripe services
        builder.Services.AddSingleton<IStripeWebhookService, StripeWebhookService>();
        builder.Services.AddSingleton<IStripeSessionService, StripeSessionService>();

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
            app.MapScalarApiReference(options =>
            {
                options.Title = "PaymentService API";
                options.Theme = ScalarTheme.Moon;
                options.Authentication = new ScalarAuthenticationOptions
                {
                    PreferredSecuritySchemes = new[] { "Bearer" }
                };
            });
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
}
// ...existing code...
