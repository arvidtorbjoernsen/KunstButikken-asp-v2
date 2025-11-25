using System.Net;
using System.Text.Json;
using KunstButikken.AuthGateway;
using KunstButikken.ServiceDefaults;

// Load .env when running the service directly
EnvLoader.LoadEnv();

var builder = WebApplication.CreateBuilder(args);

// Delegate setup to ProgramSetup
ProgramSetup.ConfigureBuilder(builder);

var app = builder.Build();

// Configure the app and run
await ProgramSetup.ConfigureApp(app).ConfigureAwait(false);

// Add startup logger for AuthGateway
Console.WriteLine("[AuthGateway] About to start app.RunAsync()...");
app.Lifetime.ApplicationStarted.Register(() => Console.WriteLine("[AuthGateway] ApplicationStarted event fired — app is running."));

await app.RunAsync().ConfigureAwait(false);
