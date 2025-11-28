using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using KunstButikken.UserService.Data;
using KunstButikken.UserService.Domain.Interfaces;
using KunstButikken.UserService.Infrastructure.Repositories;

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

        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
