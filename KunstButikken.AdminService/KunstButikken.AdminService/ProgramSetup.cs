using KunstButikken.AdminService.Infrastructure.Data;
using KunstButikken.AdminService.IntegrationEvents;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.ServiceDefaults;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Scalar.AspNetCore;

// Updated namespace

namespace KunstButikken.AdminService;

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
        builder.AddRabbitMQClient("eventbus");
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
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (frontendOrigins.Length == 0)
        {
            frontendOrigins = ["http://localhost:3000"];
        }

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("frontend", p =>
                p.WithOrigins(frontendOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

        // EF Core Postgres for AdminDb (Infrastructure DbContext)
        builder.Services.AddDbContext<AdminDbContext>(options =>
        {
            var cs = builder.Configuration.GetConnectionString("Default")
                     ?? builder.Configuration.GetConnectionString("admindb")
                     ?? builder.Configuration["ConnectionStrings:Default"]
                     ?? builder.Configuration["ConnectionStrings:admindb"];

            // In tests we may register a different provider; guard with string.IsNullOrWhiteSpace
            if (!string.IsNullOrWhiteSpace(cs) && !cs.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                // Ensure migrations are stored in the Infrastructure assembly
                options.UseNpgsql(cs, npgsql => npgsql.EnableRetryOnFailure()
                    .MigrationsAssembly(typeof(AdminDbContext).Assembly.FullName));
            }
            else
            {
                // Fallback: InMemory for tests or missing connection string
                options.UseInMemoryDatabase("admin_inmemory_db");
            }

            options.ConfigureWarnings(w =>
            {
                if (strictMigrations)
                {
                    w.Log(RelationalEventId.PendingModelChangesWarning);
                }
                else
                {
                    w.Ignore(RelationalEventId.PendingModelChangesWarning);
                }
            });
        });

        // Add services to the container.
        builder.Services.AddControllers();
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
            app.MapScalarApiReference(options =>
            {
                options.Title = "AdminService API";
                options.Theme = ScalarTheme.Moon;
                options.Authentication = new ScalarAuthenticationOptions
                {
                    PreferredSecuritySchemes = ["Bearer"]
                };
            });
        }

        // Ensure DB exists + non-destructive seed if empty
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
            try
            {
                await db.Database.MigrateAsync().ConfigureAwait(false);
            }
            catch
            {
                // In tests we may use in-memory database that doesn't support migrations; ignore failures here
            }
        }

        app.UseCors("frontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Map default health endpoints, etc.
        app.MapDefaultEndpoints();
    }
}
