using System;
using KunstButikken.AuctionService.Application.DependencyInjection;
using KunstButikken.AuctionService.Hubs;
using KunstButikken.AuctionService.IntegrationEvents;
using KunstButikken.AuctionService.IntegrationEvents.Handlers;
using KunstButikken.AuctionService.Services;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.IntegrationEvents.Contracts.Events;
using KunstButikken.ServiceDefaults;
using KunstButikken.AuctionService.Infrastructure.DependencyInjection;
using KunstButikken.AuctionService.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

using Scalar.AspNetCore;

// Updated namespace

// Load .env when running the service directly
EnvLoader.LoadEnv();

var builder = WebApplication.CreateBuilder(args);

// Toggle: set EF_STRICT_MIGRATIONS=true to log PendingModelChangesWarning instead of suppressing it
var strictMigrations =
    string.Equals(builder.Configuration["EF_STRICT_MIGRATIONS"], "true", StringComparison.OrdinalIgnoreCase);

// Add common Aspire defaults (health checks, service discovery, OpenTelemetry)
builder.AddServiceDefaults();

// Register injectable .env loader for tests/DI
builder.Services.AddEnvLoader();

// Add RabbitMQ client
builder.AddRabbitMQClient("eventbus");
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();
builder.Services.AddScoped<ArtCreatedIntegrationEventHandler>();
builder.Services.AddScoped<ArtDeletedIntegrationEventHandler>();
builder.Services.AddScoped<ArtUpdatedIntegrationEventHandler>();

// Register common system implementations
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

// Authentication (JWT Bearer via Keycloak)
var authority = builder.Configuration["KEYCLOAK_AUTHORITY"] ?? builder.Configuration["Authentication:Authority"];
var audience = builder.Configuration["KEYCLOAK_AUDIENCE"] ?? builder.Configuration["Authentication:Audience"] ??
    builder.Configuration["Authentication:ClientId"];

if (!string.IsNullOrWhiteSpace(authority) && !string.IsNullOrWhiteSpace(audience))
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority.TrimEnd('/');
            options.Audience = audience;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RoleClaimType = "roles"
            };
        });

    builder.Services.AddAuthorization();
}

// CORS for frontend origin(s)
var frontendOriginsRaw = builder.Configuration["FRONTEND_ORIGINS"] ??
                         builder.Configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";
// Split into char array and use StringSplitOptions overload
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

// Add Infrastructure and Application services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// MVC Controllers + OpenAPI
builder.Services.AddControllers();
// Register IHttpClientFactory for managed HttpClient creation
builder.Services.AddHttpClient();
// builder.Services.AddOpenApi(); // Removed

// SignalR
builder.Services.AddSignalR(options => { options.EnableDetailedErrors = true; });

// Register IAuctionSeeder and implementation so the hosted service can be thin and logic is testable
builder.Services.AddSingleton<IAuctionSeeder, AuctionSeeder>();
// Register the AuctionSeedingHostedService
builder.Services.AddHostedService<AuctionSeedingHostedService>();

var app = builder.Build();

// Enable OpenAPI/Scalar in Development or when explicitly configured
var enableOpenApi = app.Environment.IsDevelopment()
                    || string.Equals(builder.Configuration["ENABLE_OPENAPI"], "true",
                        StringComparison.OrdinalIgnoreCase);

if (enableOpenApi)
    // app.MapOpenApi(); // Removed
{
    app.MapScalarApiReference(options =>
    {
        options.Title = "AuctionService API";
        options.Theme = ScalarTheme.Moon;
        options.Authentication = new ScalarAuthenticationOptions
        {
            PreferredSecuritySchemes = new[] { "Bearer" }
        };
    });
}

// Database auto-create for dev with retry
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
    try
    {
        await db.Database.MigrateAsync().ConfigureAwait(false);
    }
    catch
    {
        // ignore migrations failure in local dev if DB not reachable
    }
}

app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SignalR hubs
app.MapHub<AuctionHub>("/hubs/auctions");

// Map default health endpoints, etc.
app.MapDefaultEndpoints();

var eventBus = app.Services.GetRequiredService<IEventBus>();
await eventBus.SubscribeAsync<ArtCreatedIntegrationEvent, ArtCreatedIntegrationEventHandler>().ConfigureAwait(false);
await eventBus.SubscribeAsync<ArtDeletedIntegrationEvent, ArtDeletedIntegrationEventHandler>().ConfigureAwait(false);
await eventBus.SubscribeAsync<ArtUpdatedIntegrationEvent, ArtUpdatedIntegrationEventHandler>().ConfigureAwait(false);

// Add startup logger so we can trace when the app enters RunAsync and when it's started
Console.WriteLine("[AuctionService] About to start app.RunAsync()...");
app.Lifetime.ApplicationStarted.Register(() => Console.WriteLine("[AuctionService] ApplicationStarted event fired — app is running."));

await app.RunAsync().ConfigureAwait(false);
