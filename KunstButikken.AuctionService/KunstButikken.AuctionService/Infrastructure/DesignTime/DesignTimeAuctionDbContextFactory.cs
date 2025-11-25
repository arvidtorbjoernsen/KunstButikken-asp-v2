using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using KunstButikken.AuctionService.Infrastructure.Persistence;

namespace KunstButikken.AuctionService.Infrastructure.DesignTime;

public class DesignTimeAuctionDbContextFactory : IDesignTimeDbContextFactory<AuctionDbContext>
{
    public AuctionDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("CONNECTIONSTRING") ?? Environment.GetEnvironmentVariable("AUCTIONS_DB__CONNECTIONSTRING");

        var optionsBuilder = new DbContextOptionsBuilder<AuctionDbContext>();
        if (string.IsNullOrWhiteSpace(cs))
        {
            // No connection string provided: use InMemory for design-time to avoid trying to connect to a DB.
            optionsBuilder.UseInMemoryDatabase("auctions_design_time");
        }
        else
        {
            optionsBuilder.UseNpgsql(cs, b => b.MigrationsAssembly(typeof(AuctionDbContext).Assembly.FullName));
        }

        return new AuctionDbContext(optionsBuilder.Options);
    }
}
