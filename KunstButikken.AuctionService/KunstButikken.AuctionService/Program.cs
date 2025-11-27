using KunstButikken.AuctionService;
using KunstButikken.ServiceDefaults;

// Load .env when running the service directly
EnvLoader.LoadEnv();

var builder = WebApplication.CreateBuilder(args);

// Delegate setup to ProgramSetup
ProgramSetup.ConfigureBuilder(builder);

var app = builder.Build();

// Configure the app and run
await ProgramSetup.ConfigureApp(app).ConfigureAwait(false);

// Add startup logger for AuctionService
Console.WriteLine("[AuctionService] About to start app.RunAsync()...");
app.Lifetime.ApplicationStarted.Register(() => Console.WriteLine("[AuctionService] ApplicationStarted event fired — app is running."));

await app.RunAsync().ConfigureAwait(false);
