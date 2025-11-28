using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using KunstButikken.ServiceDefaults;
using KunstButikken.UserService.Application.Interfaces;
using KunstButikken.UserService.Application.Services;

namespace KunstButikken.UserService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IAdminProfileService, AdminProfileService>();
        services.AddScoped<ISellerQueryService, SellerQueryService>();
        return services;
    }
}
