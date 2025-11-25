// ...existing code...
using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using KunstButikken.PaymentService.Data;

namespace KunstButikken.PaymentService.Infrastructure.DesignTime;

public class DesignTimePaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    public PaymentDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("CONNECTIONSTRING")
                 ?? Environment.GetEnvironmentVariable("PAYMENT_DB__CONNECTIONSTRING") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cs))
        {
            cs = "Host=localhost;Database=payment_db;Username=postgres;Password=postgres";
        }

        var optionsBuilder = new DbContextOptionsBuilder<PaymentDbContext>();
        optionsBuilder.UseNpgsql(cs, b => b.MigrationsAssembly(typeof(PaymentDbContext).Assembly.FullName));

        return new PaymentDbContext(optionsBuilder.Options);
    }
}

