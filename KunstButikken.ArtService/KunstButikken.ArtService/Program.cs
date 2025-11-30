using KunstButikken.ArtService;
using KunstButikken.ArtService.Infrastructure.Data;
using KunstButikken.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

// Load .env when running the service directly
EnvLoader.LoadEnv();

var builder = WebApplication.CreateBuilder(args);

// Delegate setup to ProgramSetup
ProgramSetup.ConfigureBuilder(builder);

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ArtDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

// Configure the app and run
await ProgramSetup.ConfigureApp(app).ConfigureAwait(false);

// Add startup logger for ArtService
Console.WriteLine("[ArtService] About to start app.RunAsync()...");
app.Lifetime.ApplicationStarted.Register(() => Console.WriteLine("[ArtService] ApplicationStarted event fired — app is running."));

await app.RunAsync().ConfigureAwait(false);
