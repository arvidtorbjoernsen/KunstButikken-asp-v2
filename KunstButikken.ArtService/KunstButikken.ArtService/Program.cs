using KunstButikken.ArtService.IntegrationEvents;
using KunstButikken.ArtService.Services;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.ServiceDefaults;

using Microsoft.Extensions.Azure;

using Scalar.Aspire;

using IBlobStorage = KunstButikken.ArtService.Domain.Interfaces.IBlobStorage;

// Added DI extension namespaces and logging
using KunstButikken.ArtService.Application.DependencyInjection;
using KunstButikken.ArtService.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Logging;

// Reflection helpers
using System;
using System.Linq;
using System.Reflection;

// Updated namespace

// needed for Guid, DateTimeOffset, Console
// needed for file IO and Path
// for GetService<T>()

// Load .env when running the service directly
EnvLoader.LoadEnv();

var builder = WebApplication.CreateBuilder(args);

// Reduce EF Core command noise in development: only warnings and above
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

// Seed locale: use `Seed:Locale` config or `SEED_LOCALE` env var. If it starts with "no" we use Norwegian metadata fields.
var seedLocale = builder.Configuration["Seed:Locale"] ?? builder.Configuration["SEED_LOCALE"] ?? "en";
var useNorwegian = seedLocale.StartsWith("no", StringComparison.OrdinalIgnoreCase);

// Toggle: set EF_STRICT_MIGRATIONS=true to log PendingModelChangesWarning instead of suppressing it
// var strictMigrations = string.Equals(builder.Configuration["EF_STRICT_MIGRATIONS"], "true", StringComparison.OrdinalIgnoreCase);

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

// JWT auth via ServiceDefaults
// Authentication intentionally disabled: remove JWT auth registration for development/no-auth runs

// CORS for frontend origin(s)
var frontendOriginsRaw = builder.Configuration["FRONTEND_ORIGINS"] ??
                         builder.Configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";
var frontendOrigins = frontendOriginsRaw
    .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .ToList();

// In development, always include localhost:4200 (Angular) and localhost:3000 (Next.js) to prevent CORS issues
if (builder.Environment.IsDevelopment())
{
    if (!frontendOrigins.Contains("http://localhost:4200"))
    {
        frontendOrigins.Add("http://localhost:4200");
    }

    if (!frontendOrigins.Contains("http://localhost:3000"))
    {
        frontendOrigins.Add("http://localhost:3000");
    }
}

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

// Add Infrastructure and Application layers (DbContext, repositories, application services)
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Azure Blob storage for images
var azureBlobConnString = builder.Configuration["AzureBlob:ConnectionString"];
if (!string.IsNullOrEmpty(azureBlobConnString))
{
    builder.Services.AddAzureClients(clientBuilder => { clientBuilder.AddBlobServiceClient(azureBlobConnString); });
    builder.Services.AddSingleton<IBlobStorage, BlobStorage>();
}
else
{
    Console.WriteLine("[ArtService] AzureBlob:ConnectionString not found. Using NullBlobStorage.");
    builder.Services.AddSingleton<IBlobStorage, NullBlobStorage>();
}

// Add services to the container.
builder.Services.AddControllers();
// Add OpenAPI/Scalar documentation
// builder.Services.AddOpenApi(); // Removed

// Register the Art seeder implementation and hosted service wrapper
builder.Services.AddSingleton<IArtSeeder, ArtSeeder>();
// Register the ArtSeedingHostedService
builder.Services.AddHostedService<ArtSeedingHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable OpenAPI/Scalar in Development or when explicitly configured
var enableOpenApi = app.Environment.IsDevelopment()
                    || string.Equals(builder.Configuration["ENABLE_OPENAPI"], "true",
                        StringComparison.OrdinalIgnoreCase);

if (enableOpenApi)
    // app.MapOpenApi(); // Removed
{
    // Use reflective mapper to avoid compile-time dependency on Scalar API surface
    TryMapScalarApiReference(app);
}

app.UseCors("frontend");
// Authentication/Authorization middleware removed (no-op public APIs)
app.MapControllers();

// Map default health endpoints, etc.
app.MapDefaultEndpoints();

// Add simple startup logger so we can trace when the app enters RunAsync and when it's started
Console.WriteLine("[ArtService] About to start app.RunAsync()...");
app.Lifetime.ApplicationStarted.Register(() => Console.WriteLine("[ArtService] ApplicationStarted event fired — app is running."));

await app.RunAsync().ConfigureAwait(false);


// Reflection-based Scalar API mapper (mirrors AdminService approach)
static void TryMapScalarApiReference(WebApplication app)
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
            Console.WriteLine("[ArtService] MapScalarApiReference extension not found (Scalar package may not expose it). Skipping Scalar API reference mapping.");
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
                Console.WriteLine("[ArtService] Invoked MapScalarApiReference(WebApplication)");
                return;
            }

            if (ps.Length == 2 && ps[0].ParameterType.IsAssignableFrom(appType))
            {
                // method signature: MapScalarApiReference(WebApplication, Action<Options>)
                // Try invoking with null for options (some Scalar packages accept null)
                try
                {
                    m.Invoke(null, new object[] { app, null });
                    Console.WriteLine("[ArtService] Invoked MapScalarApiReference(WebApplication, options) with null options");
                    return;
                }
                catch
                {
                    // ignore and try next
                }
            }
        }

        Console.WriteLine("[ArtService] No matching MapScalarApiReference overload found; skipping.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("[ArtService] Error invoking MapScalarApiReference reflectively: " + ex.Message);
    }
}
