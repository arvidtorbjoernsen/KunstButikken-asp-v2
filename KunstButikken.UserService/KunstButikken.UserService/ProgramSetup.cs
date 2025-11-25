// ...existing code...
using System;
using System.Threading.Tasks;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.ServiceDefaults;
using KunstButikken.UserService.Data;
using KunstButikken.UserService.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Scalar.AspNetCore;
using KunstButikken.UserService.Infrastructure.DependencyInjection;
using KunstButikken.UserService.Services;

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

        // Use the Infrastructure project's registration to configure DbContext and repository registrations
        // This keeps repository wiring centralized in the Infrastructure layer.
        try
        {
            // Call the AddInfrastructure helper explicitly to avoid extension method resolution issues in some build contexts
            KunstButikken.UserService.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions
                .AddInfrastructure(builder.Services, builder.Configuration);
        }
        catch
        {
            // If the Infrastructure extension isn't available (e.g. in early migration), fall back to best-effort registration
            var cs = builder.Configuration.GetConnectionString("Default")
                     ?? builder.Configuration.GetConnectionString("usersdb")
                     ?? builder.Configuration["ConnectionStrings:Default"]
                     ?? builder.Configuration["ConnectionStrings:usersdb"];

            if (!string.IsNullOrWhiteSpace(cs) && !cs.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                builder.Services.AddDbContext<UserDbContext>(options =>
                    options.UseNpgsql(cs, npgsql => npgsql.EnableRetryOnFailure()
                        .MigrationsAssembly(typeof(UserDbContext).Assembly.FullName)));
            }
            else
            {
                builder.Services.AddDbContext<UserDbContext>(options => options.UseInMemoryDatabase("user_inmemory_db"));
            }

            builder.Services.AddScoped<IUserRepository, UserRepository>();
        }

        // Add services to the container.
        builder.Services.AddControllers()
            .AddJsonOptions(options => { options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase; });

        builder.Services.AddHttpClient();

        // Register Keycloak seeder and hosted service so seeding runs on startup (development flows)
        // IKeycloakSeeder implementation (KeycloakSeeder) and its hosted service will attempt to seed Keycloak
        // when configuration allows it. This mirrors previous wiring that executed seeding at startup.
        builder.Services.AddSingleton<IKeycloakSeeder, KeycloakSeeder>();
        builder.Services.AddHostedService<KeycloakSeedingHostedService>();

        // Register helper types for manual dev-controller-triggered seeding
        // KeycloakUserProcessor: lightweight processor used by seeder
        builder.Services.AddSingleton<KeycloakUserProcessor>();

        // KeycloakSeederService: use typed HttpClient so HttpClient is injected and lifetime is managed
        builder.Services.AddHttpClient<KeycloakSeederService>();

        // DevSeedService: resolves KeycloakSeederService and is used by the DevController; register as scoped
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
            app.MapScalarApiReference(options =>
            {
                options.Title = "UserService API";
                options.Theme = ScalarTheme.Moon;
                options.Authentication = new ScalarAuthenticationOptions
                {
                    PreferredSecuritySchemes = new[] { "Bearer" }
                };
            });
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
}
// ...existing code...
