using KunstButikken.PaymentService.Application.DependencyInjection;
using KunstButikken.PaymentService.Application.Interfaces;
using KunstButikken.PaymentService.Domain.Repositories;
using KunstButikken.PaymentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KunstButikken.PaymentService.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Default")
                 ?? configuration.GetConnectionString("paymentsdb")
                 ?? configuration["ConnectionStrings:Default"]
                 ?? configuration["ConnectionStrings:paymentsdb"];

        if (!string.IsNullOrWhiteSpace(cs) && !cs.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<PaymentDbContext>(options =>
                options.UseNpgsql(cs, npgsql => npgsql.EnableRetryOnFailure()
                    .MigrationsAssembly(typeof(PaymentDbContext).Assembly.FullName)));
        }
        else
        {
            services.AddDbContext<PaymentDbContext>(options => options.UseInMemoryDatabase("payment_inmemory_db"));
        }

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddApplication();
        return services;
    }
}
