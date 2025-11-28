using System;
using System.Linq;
using System.Text.Json;
using KunstButikken.AuthGateway.DependencyInjection;
using KunstButikken.ServiceDefaults;
using Scalar.Aspire;

namespace KunstButikken.AuthGateway;

public static class ProgramSetup
{
    public static void ConfigureBuilder(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddServiceDefaults();
        builder.Services.AddEnvLoader();
        builder.Services.AddApplication(builder.Configuration, builder.Environment);

        var frontendOriginsRaw = builder.Configuration["FRONTEND_ORIGINS"] ?? builder.Configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";
        var frontendOrigins = frontendOriginsRaw
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

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

        var realm = builder.Configuration["KEYCLOAK_REALM"] ?? "kunstbutikken";
        var audience = builder.Configuration["KEYCLOAK_AUDIENCE"] ?? builder.Configuration["Authentication:Audience"] ?? "kunstbutikken-api";
        var authority = builder.Configuration["KEYCLOAK_AUTHORITY"] ?? builder.Configuration["Authentication:Authority"];

        if (!string.IsNullOrWhiteSpace(authority) || !string.IsNullOrWhiteSpace(realm))
        {
            builder.Services
                .AddAuthentication()
                .AddKeycloakJwtBearer("keycloak", realm: realm, options =>
                {
                    options.Audience = audience;
                    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (builder.Environment.IsDevelopment())
                            {
                                Console.WriteLine($"[AuthGateway] Authentication failed: {context.Exception.Message}");
                            }

                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            if (builder.Environment.IsDevelopment())
                            {
                                Console.WriteLine($"[AuthGateway] Token validated successfully for user: {context.Principal?.Identity?.Name}");
                            }

                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            if (builder.Environment.IsDevelopment())
                            {
                                Console.WriteLine($"[AuthGateway] Authentication challenge: {context.Error}, {context.ErrorDescription}");
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();
        }
        else
        {
            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();
        }
    }

    public static async Task ConfigureApp(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseRouting();

        app.Use(async (context, next) =>
        {
            var origin = context.Request.Headers["Origin"].ToString();
            if (!string.IsNullOrEmpty(origin)) Console.WriteLine($"[AuthGateway][CORS Debug] Incoming Request Origin: {origin}");
            await next().ConfigureAwait(false);
        });

        app.UseCors("frontend");
        app.UseWebSockets();

        if (app.Environment.IsDevelopment())
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/api/profile", StringComparison.Ordinal))
                {
                    var hasAuth = context.Request.Headers.TryGetValue("Authorization", out var authHeader);
                    Console.WriteLine($"[AuthGateway][Debug] Request to {context.Request.Path}, Authorization header present: {hasAuth}");
                    if (hasAuth)
                    {
                        var authValue = authHeader.ToString();
                        var tokenPreview = authValue.Length > 50 ? string.Concat(authValue.AsSpan(0, 50), "...") : authValue;
                        Console.WriteLine($"[AuthGateway][Debug] Authorization: {tokenPreview}");
                    }
                }

                await next().ConfigureAwait(false);
            });

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/hubs/auctions", StringComparison.Ordinal))
                {
                    Console.WriteLine($"[AuthGateway][Hub] Incoming {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
                    var origin = context.Request.Headers["Origin"].ToString();
                    if (!string.IsNullOrEmpty(origin)) Console.WriteLine($"[AuthGateway][Hub] Origin: {origin}");
                }

                await next().ConfigureAwait(false);
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapReverseProxy();
        app.MapDefaultEndpoints();

        await Task.CompletedTask;
    }
}
