using KunstButikken.AdminService.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ApplicationAdminService = KunstButikken.AdminService.Application.Services.AdminService;

namespace KunstButikken.AdminService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAdminApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IAdminService, ApplicationAdminService>();
        return services;
    }
}
