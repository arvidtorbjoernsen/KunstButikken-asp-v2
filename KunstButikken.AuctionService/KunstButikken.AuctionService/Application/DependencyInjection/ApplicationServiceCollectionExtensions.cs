using Microsoft.Extensions.DependencyInjection;
using KunstButikken.AuctionService.Application.Interfaces;
using KunstButikken.AuctionService.Application.Services;
using KunstButikken.AuctionService.Domain.Interfaces;
using KunstButikken.ServiceDefaults;

namespace KunstButikken.AuctionService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IAuctionService, AuctionAppService>();
        services.AddScoped<IAuctionSeeder, AuctionSeeder>();
        services.AddHostedService<AuctionSeedingHostedService>();
        return services;
    }
}
