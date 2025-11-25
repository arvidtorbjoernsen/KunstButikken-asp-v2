using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using KunstButikken.AdminService.Infrastructure.Data;

namespace KunstButikken.AdminService.Infrastructure.DesignTime;

public class DesignTimeAdminDbContextFactory : IDesignTimeDbContextFactory<AdminDbContext>
{
    public AdminDbContext CreateDbContext(string[] args)
    {
        // Prefer environment variable first; this allows CLI tooling to set connection string.
        var cs = Environment.GetEnvironmentVariable("CONNECTIONSTRING") ?? Environment.GetEnvironmentVariable("ADMIN_DB__CONNECTIONSTRING") ?? string.Empty;
        // Also try ASP.NET Core configuration if appsettings.json is present in the web project folder
        try
        {
            var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "KunstButikken.AdminService"));
            var jsonPath = Path.Combine(basePath, "appsettings.json");
            if (File.Exists(jsonPath))
            {
                var json = File.ReadAllText(jsonPath);
                // crude but avoids needing IConfiguration packages: look for "ConnectionStrings" -> "Default"
                var marker = "\"ConnectionStrings\"";
                if (json.Contains(marker, StringComparison.OrdinalIgnoreCase))
                {
                    // Not a robust JSON parse; if present, prefer environment variables anyway.
                }
            }
        }
        catch
        {
            // ignore
        }

        var optionsBuilder = new DbContextOptionsBuilder<AdminDbContext>();
        if (string.IsNullOrWhiteSpace(cs))
        {
            // No env var set — for design-time we still need a relational provider so migrations tooling can register IMigrator.
            // Fall back to a local Postgres connection string (developer machine). Update if you use another dev DB.
            cs = "Host=localhost;Database=admin_db;Username=postgres;Password=postgres";
        }

        // Use Npgsql to ensure relational migrations services are available at design-time
        optionsBuilder.UseNpgsql(cs, b => b.MigrationsAssembly(typeof(AdminDbContext).Assembly.FullName));

        return new AdminDbContext(optionsBuilder.Options);
    }
}
