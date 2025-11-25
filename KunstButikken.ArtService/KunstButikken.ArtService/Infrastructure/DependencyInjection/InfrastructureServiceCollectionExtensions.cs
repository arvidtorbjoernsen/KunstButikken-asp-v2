// new file content
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using KunstButikken.ArtService.Infrastructure.Data;

namespace KunstButikken.ArtService.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Default")
                 ?? configuration.GetConnectionString("artdb");

        if (!string.IsNullOrWhiteSpace(cs) && !cs.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<ArtDbContext>(options =>
            {
                options.UseNpgsql(cs, b => b.MigrationsAssembly(typeof(ArtDbContext).Assembly.FullName));
            });
        }
        else
        {
            services.AddDbContext<ArtDbContext>(options => options.UseInMemoryDatabase("art_inmemory"));
        }

        services.AddScoped<KunstButikken.ArtService.Domain.Interfaces.IArtRepository, KunstButikken.ArtService.Infrastructure.Repositories.ArtRepository>();
        return services;
    }
}

