using KunstButikken.AdminService.Domain.Interfaces;
using KunstButikken.AdminService.Infrastructure.Data;
using KunstButikken.AdminService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KunstButikken.AdminService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAdminInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool strictMigrations = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<AdminDbContext>(options =>
        {
            var cs = configuration.GetConnectionString("Default")
                     ?? configuration.GetConnectionString("admindb")
                     ?? configuration["ConnectionStrings:Default"]
                     ?? configuration["ConnectionStrings:admindb"]
                     ?? "InMemory";

            if (cs.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                options.UseInMemoryDatabase("admin_inmemory_db");
            }
            else
            {
                options.UseNpgsql(cs, npgsql =>
                    npgsql.EnableRetryOnFailure()
                          .MigrationsAssembly(typeof(AdminDbContext).Assembly.FullName));
            }

            options.ConfigureWarnings(w =>
            {
                if (strictMigrations)
                {
                    w.Log(RelationalEventId.PendingModelChangesWarning);
                }
                else
                {
                    w.Ignore(RelationalEventId.PendingModelChangesWarning);
                }
            });
        });

        services.AddScoped<IAdminRepository, AdminRepository>();
        return services;
    }
}
