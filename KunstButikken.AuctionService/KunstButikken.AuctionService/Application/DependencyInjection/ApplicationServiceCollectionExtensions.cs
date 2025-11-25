using Microsoft.Extensions.DependencyInjection;
using KunstButikken.AuctionService.Application.Interfaces;

namespace KunstButikken.AuctionService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuctionService, KunstButikken.AuctionService.Application.Services.AuctionAppService>();
        return services;
    }
}
