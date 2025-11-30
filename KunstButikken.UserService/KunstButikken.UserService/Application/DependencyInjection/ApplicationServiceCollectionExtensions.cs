using Microsoft.Extensions.DependencyInjection;
using KunstButikken.UserService.Application.Interfaces;
using KunstButikken.UserService.Application.Services;
using KunstButikken.ServiceDefaults;
using Microsoft.Extensions.Configuration;

namespace KunstButikken.UserService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IAdminProfileService, AdminProfileService>();
        services.AddScoped<ISellerQueryService, SellerQueryService>();
        services.AddScoped<IDevSeedService, DevSeedService>();
        services.AddScoped<IKeycloakSyncService, KeycloakSyncApplicationService>();
        return services;
    }
}
