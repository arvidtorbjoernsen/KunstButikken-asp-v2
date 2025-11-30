using System;
using System.IO;

using KunstButikken.ArtService.Infrastructure.Data;


using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;


namespace KunstButikken.ArtService.Infrastructure.DesignTime;

public class DesignTimeArtDbContextFactory : IDesignTimeDbContextFactory<ArtDbContext>
{
    public ArtDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("CONNECTIONSTRING")
                 ?? Environment.GetEnvironmentVariable("ART_DB__CONNECTIONSTRING")
                 ?? BuildConnectionStringFromConfig()
                 ?? "Host=localhost;Database=artdb;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ArtDbContext>();
        optionsBuilder.UseNpgsql(cs, b => b.MigrationsAssembly(typeof(ArtDbContext).Assembly.FullName));

        return new ArtDbContext(optionsBuilder.Options);
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
            return config.GetConnectionString("Default")
                   ?? config.GetConnectionString("artdb");
        }
        catch
        {
            return null;
        }
    }
}
