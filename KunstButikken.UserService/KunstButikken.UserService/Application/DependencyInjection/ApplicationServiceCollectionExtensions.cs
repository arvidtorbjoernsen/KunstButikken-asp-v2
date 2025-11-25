using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace KunstButikken.UserService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application services here when they exist (e.g. IUserService)
        return services;
    }
}
