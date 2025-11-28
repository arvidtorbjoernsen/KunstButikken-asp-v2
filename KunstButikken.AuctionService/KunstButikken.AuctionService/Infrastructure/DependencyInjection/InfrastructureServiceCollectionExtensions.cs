using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using KunstButikken.AuctionService.Infrastructure.Persistence;
using KunstButikken.AuctionService.Domain.Interfaces;
using KunstButikken.AuctionService.Infrastructure.Repositories;

namespace KunstButikken.AuctionService.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Default")
                 ?? configuration.GetConnectionString("auctionsdb");

        if (!string.IsNullOrWhiteSpace(cs) && !cs.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<AuctionDbContext>(options =>
            {
                options.UseNpgsql(cs, b => b.MigrationsAssembly(typeof(AuctionDbContext).Assembly.FullName));
            });
        }
        else
        {
            services.AddDbContext<AuctionDbContext>(options => options.UseInMemoryDatabase("auctions_inmemory"));
        }

        services.AddScoped<IAuctionRepository, AuctionRepository>();
        return services;
    }
}
