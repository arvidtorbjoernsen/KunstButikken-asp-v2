using KunstButikken.AuthGateway.Application.AuthSessions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KunstButikken.AuthGateway.Application.DependencyInjection;

public static class AuthSessionServiceCollectionExtensions
{
    public static IServiceCollection AddAuthSessions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthSessionOptions>(configuration.GetSection("AuthSession"));
        services.AddSingleton<IAuthSessionStore, InMemoryAuthSessionStore>();
        services.AddSingleton<IAuthCookieWriter, CookieWriter>();
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddHttpClient(nameof(KeycloakTokenClient));
        services.AddScoped<IKeycloakTokenClient, KeycloakTokenClient>();
        return services;
    }
}
