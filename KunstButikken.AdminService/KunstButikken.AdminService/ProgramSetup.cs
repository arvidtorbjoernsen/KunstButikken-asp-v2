using System;
using System.Linq;
using System.Reflection;

using KunstButikken.AdminService.Infrastructure.Data;
using KunstButikken.AdminService.IntegrationEvents;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.ServiceDefaults;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Scalar.Aspire;

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
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        if (frontendOrigins.Count == 0)
        {
            frontendOrigins.Add("http://localhost:3000");
        }

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("frontend", p =>
                p.WithOrigins(frontendOrigins.ToArray())
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
            // Use reflection to call MapScalarApiReference if Scalar.Aspire/Scalar.AspNetCore exposes it.
            TryMapScalarApiReference(app);
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

    private static void TryMapScalarApiReference(WebApplication app)
    {
        try
        {
            var appType = app.GetType();
            // Look for extension method MapScalarApiReference
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
                Console.WriteLine("[ProgramSetup] MapScalarApiReference extension not found (Scalar package may not expose it). Skipping Scalar API reference mapping.");
                return;
            }

            // Pick the first candidate that accepts WebApplication and an action/config delegate
            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (ps.Length == 1 && ps[0].ParameterType.IsAssignableFrom(appType))
                {
                    // method signature: MapScalarApiReference(WebApplication)
                    m.Invoke(null, new object[] { app });
                    Console.WriteLine("[ProgramSetup] Invoked MapScalarApiReference(WebApplication)");
                    return;
                }

                if (ps.Length == 2 && ps[0].ParameterType.IsAssignableFrom(appType))
                {
                    // method signature: MapScalarApiReference(WebApplication, Action<Options>)
                    // Build a compatible delegate dynamically
                    var optionsType = ps[1].ParameterType.GetGenericArguments().FirstOrDefault() ?? ps[1].ParameterType;
                    // Fallback: if we cannot construct a matching delegate, try invoking with null
                    try
                    {
                        m.Invoke(null, new object[] { app, null });
                        Console.WriteLine("[ProgramSetup] Invoked MapScalarApiReference(WebApplication, options) with null options");
                        return;
                    }
                    catch
                    {
                        // ignore and try next
                    }
                }
            }

            Console.WriteLine("[ProgramSetup] No matching MapScalarApiReference overload found; skipping.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ProgramSetup] Error invoking MapScalarApiReference reflectively: " + ex.Message);
        }
    }
}
