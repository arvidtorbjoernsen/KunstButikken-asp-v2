using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using KunstButikken.AuctionService.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace KunstButikken.AuctionService.Infrastructure.DesignTime;

public class DesignTimeAuctionDbContextFactory : IDesignTimeDbContextFactory<AuctionDbContext>
{
    public AuctionDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("CONNECTIONSTRING")
                 ?? Environment.GetEnvironmentVariable("AUCTIONS_DB__CONNECTIONSTRING")
                 ?? BuildConnectionStringFromConfig()
                 ?? "Host=localhost;Database=auctionsdb;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AuctionDbContext>();
        optionsBuilder.UseNpgsql(cs, b => b.MigrationsAssembly(typeof(AuctionDbContext).Assembly.FullName));

        return new AuctionDbContext(optionsBuilder.Options);
    }

    private static string? BuildConnectionStringFromConfig()
    {
        try
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."))
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false);

            var config = builder.Build();
            return config.GetConnectionString("Default");
        }
        catch
        {
            return null;
        }
    }
}
