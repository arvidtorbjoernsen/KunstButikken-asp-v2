using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using KunstButikken.UserService.Domain.Interfaces;
using KunstButikken.UserService.Infrastructure.Persistence;
using KunstButikken.UserService.Infrastructure.Repositories;
using KunstButikken.UserService.Application.Interfaces;
using KunstButikken.UserService.Infrastructure.Keycloak.Seeding;
using KunstButikken.UserService.Infrastructure.Keycloak.Sync;

namespace KunstButikken.UserService.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Default")
                 ?? configuration.GetConnectionString("usersdb");

        if (!string.IsNullOrWhiteSpace(cs) && !cs.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<UserDbContext>(options =>
            {
                options.UseNpgsql(cs, b => b.MigrationsAssembly(typeof(UserDbContext).Assembly.FullName));
            });
        }
        else
        {
            services.AddDbContext<UserDbContext>(options => options.UseInMemoryDatabase("users_inmemory"));
        }

        services.AddHttpClient();
        services.AddSingleton<KeycloakUserProcessor>();
        services.AddSingleton<IKeycloakAdminClient, KeycloakAdminClient>();
        services.AddSingleton<KeycloakSyncService>();
        services.AddSingleton<IKeycloakSyncRunner>(sp => sp.GetRequiredService<KeycloakSyncService>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<KeycloakSyncService>());
        services.AddScoped<IDevKeycloakSeeder, KeycloakSeederService>();
        services.AddScoped<IDbMigrationRunner, DbMigrationRunner>();
        services.AddScoped<IKeycloakSeeder, KeycloakSeeder>();
        services.AddHostedService<DbMigrationHostedService>();
        services.AddHostedService<KeycloakSeedingHostedService>();

        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
