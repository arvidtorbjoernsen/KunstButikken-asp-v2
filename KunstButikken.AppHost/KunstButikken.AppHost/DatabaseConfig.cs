namespace KunstButikken.AppHost;

using Aspire.Hosting;

internal static class DatabaseConfig
{
    public static (object postgres, object usersDb, object artDb, object auctionsDb, object paymentsDb, object adminDb) Configure(IDistributedApplicationBuilder builder)
    {
        var postgres = builder.AddPostgres("postgres").WithDataVolume().WithPgAdmin(c => c.WithHostPort(4711));
        var usersDb = postgres.AddDatabase("usersdb");
        var artDb = postgres.AddDatabase("artdb");
        var auctionsDb = postgres.AddDatabase("auctionsdb");
        var paymentsDb = postgres.AddDatabase("paymentsdb");
        var adminDb = postgres.AddDatabase("admindb");
        return (postgres, usersDb, artDb, auctionsDb, paymentsDb, adminDb);
    }
}

