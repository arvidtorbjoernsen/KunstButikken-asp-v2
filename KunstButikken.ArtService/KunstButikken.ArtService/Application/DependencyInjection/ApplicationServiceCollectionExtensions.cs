using KunstButikken.ArtService.Application.Interfaces;
using KunstButikken.ArtService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using AppArtService = KunstButikken.ArtService.Application.Services.ArtService;

namespace KunstButikken.ArtService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IArtService, AppArtService>();
        services.AddSingleton<IArtSeeder, ArtSeeder>();
        services.AddHostedService<ArtSeedingHostedService>();
        return services;
    }
}
