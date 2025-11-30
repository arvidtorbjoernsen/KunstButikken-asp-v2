using System;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using KunstButikken.ServiceDefaults;
using KunstButikken.UserService.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Scalar.Aspire;
using Microsoft.IdentityModel.Tokens;
using KunstButikken.UserService.Infrastructure.DependencyInjection;
using KunstButikken.UserService.Application.DependencyInjection;
using KunstButikken.UserService.Infrastructure.Keycloak.Seeding;
using KunstButikken.UserService.Infrastructure.Keycloak.Sync;
using KunstButikken.UserService.Infrastructure.Persistence;

namespace KunstButikken.UserService;

public static class ProgramSetup
{
    public static void ConfigureBuilder(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddServiceDefaults();
        builder.Services.AddEnvLoader();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddApplication(builder.Configuration);

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

        builder.Services.AddControllers()
            .AddJsonOptions(options => { options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase; });

        var realm = builder.Configuration["KEYCLOAK_REALM"] ?? "kunstbutikken";
        var fallbackAudience = builder.Configuration["KEYCLOAK_AUDIENCE"] ?? builder.Configuration["Authentication:Audience"];
        var audiences = ParseAudiences(builder.Configuration["KEYCLOAK_AUDIENCES"], fallbackAudience ?? "kunstbutikken-api");

        var authority = builder.Configuration["KEYCLOAK_AUTHORITY"] ?? builder.Configuration["Authentication:Authority"];

        var keycloakIssuer = builder.Configuration["KEYCLOAK_ISSUER"] ?? builder.Configuration["NEXT_PUBLIC_KEYCLOAK_ISSUER"] ?? string.Empty;

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
                    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                    {
                        OnAuthenticationFailed = context => { Console.WriteLine($"[UserService] Authentication failed: {context.Exception.Message}"); return Task.CompletedTask; },
                        OnTokenValidated = context => { Console.WriteLine($"[UserService] Token validated successfully for user: {context.Principal?.Identity?.Name}"); return Task.CompletedTask; }
                    };
                });

            builder.Services.AddAuthorization();
        }
        else
        {
            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();
        }

        // Ensure ProgramSetup uses new hosted services for seeding and migration.
        builder.Services.AddHostedService<DbMigrationHostedService>();
        builder.Services.AddHostedService<KeycloakSeedingHostedService>();
        builder.Services.AddHostedService<KeycloakSyncService>();
    }

    public static async Task ConfigureApp(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var enableOpenApi = app.Environment.IsDevelopment()
                            || string.Equals(app.Configuration["ENABLE_OPENAPI"], "true",
                                StringComparison.OrdinalIgnoreCase);

        if (enableOpenApi)
        {
            TryMapScalarApiReference(app);
        }

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
                }
            }
            catch
            {
            }
        }

        app.UseCors("frontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapDefaultEndpoints();
    }

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
