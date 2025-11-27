using KunstButikken.Common.Logging;
using KunstButikken.PaymentService;
using KunstButikken.ServiceDefaults;
using Scalar.Aspire;

// Load .env when running the service directly
EnvLoader.LoadEnv();

var builder = WebApplication.CreateBuilder(args);

// Delegate setup to ProgramSetup
ProgramSetup.ConfigureBuilder(builder);

var app = builder.Build();

// Configure the app and run
await ProgramSetup.ConfigureApp(app).ConfigureAwait(false);

// Add startup logger for PaymentService
Console.WriteLine("[PaymentService] About to start app.RunAsync()...");
app.Lifetime.ApplicationStarted.Register(() => Console.WriteLine("[PaymentService] ApplicationStarted event fired — app is running."));

await app.RunAsync().ConfigureAwait(false);
