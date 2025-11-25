// new file content
using Microsoft.Extensions.DependencyInjection;
using KunstButikken.ArtService.Application.Interfaces;

namespace KunstButikken.ArtService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IArtService, KunstButikken.ArtService.Application.Services.ArtService>();
        return services;
    }
}

