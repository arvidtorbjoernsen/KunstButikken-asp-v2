using KunstButikken.Common.Logging;
using KunstButikken.UserService.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KunstButikken.UserService.Services;

public class DbMigrationRunner(IServiceProvider serviceProvider, ILogger<DbMigrationRunner> logger)
    : IDbMigrationRunner
{
    public async Task RunMigrationsAsync(CancellationToken ct = default)
    {
        LogMessages.Information_Msg_13(logger, null);
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();

        try
        {
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync(ct).ConfigureAwait(false);
            if (pendingMigrations.Any())
            {
                LogMessages.Information_Msg_14(logger, null);
                await db.Database.MigrateAsync(ct).ConfigureAwait(false);
                LogMessages.Information_Msg_15(logger, null);
            }
            else
            {
                LogMessages.Information_Msg_16(logger, null);
                await db.Database.EnsureCreatedAsync(ct).ConfigureAwait(false);
            }
        }
        catch (PostgresException pex)
        {
            LogMessages.Error_Message_17(logger, pex.MessageText, pex);
        }
        catch (Exception ex)
        {
            LogMessages.Error_Message_18(logger, ex.Message, ex);
        }
    }
}
