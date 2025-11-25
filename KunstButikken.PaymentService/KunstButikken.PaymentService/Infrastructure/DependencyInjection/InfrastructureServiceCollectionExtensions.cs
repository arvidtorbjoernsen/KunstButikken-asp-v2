// ...existing code...
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using KunstButikken.PaymentService.Data;

namespace KunstButikken.PaymentService.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Default")
                 ?? configuration.GetConnectionString("paymentsdb");

        if (!string.IsNullOrWhiteSpace(cs) && !cs.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<PaymentDbContext>(options =>
            {
                options.UseNpgsql(cs, b => b.MigrationsAssembly(typeof(PaymentDbContext).Assembly.FullName));
            });
        }
        else
        {
            services.AddDbContext<PaymentDbContext>(options => options.UseInMemoryDatabase("payment_inmemory"));
        }

        services.AddScoped<KunstButikken.PaymentService.Data.IPaymentRepository, KunstButikken.PaymentService.Data.PaymentRepository>();
        return services;
    }
}

