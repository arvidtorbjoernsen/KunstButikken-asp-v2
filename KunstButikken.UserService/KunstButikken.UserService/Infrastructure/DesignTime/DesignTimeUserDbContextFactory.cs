using System;
using System.IO;

using KunstButikken.UserService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KunstButikken.UserService.Infrastructure.DesignTime;

public class DesignTimeUserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("CONNECTIONSTRING")
                 ?? Environment.GetEnvironmentVariable("USER_DB__CONNECTIONSTRING") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cs))
        {
            cs = "Host=localhost;Database=user_db;Username=postgres;Password=postgres";
        }

        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        optionsBuilder.UseNpgsql(cs, b => b.MigrationsAssembly(typeof(UserDbContext).Assembly.FullName));

        return new UserDbContext(optionsBuilder.Options);
    }
}
