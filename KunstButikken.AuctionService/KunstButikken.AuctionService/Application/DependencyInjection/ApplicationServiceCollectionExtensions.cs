using Microsoft.Extensions.DependencyInjection;
using KunstButikken.AuctionService.Application.Interfaces;
using KunstButikken.AuctionService.Application.Services;
using KunstButikken.ServiceDefaults;

namespace KunstButikken.AuctionService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IAuctionService, AuctionAppService>();
        return services;
    }
}
