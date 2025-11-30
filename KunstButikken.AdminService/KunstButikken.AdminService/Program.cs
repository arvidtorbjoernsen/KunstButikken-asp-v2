using KunstButikken.AdminService;
using KunstButikken.ServiceDefaults;

// Load .env when running the service directly
EnvLoader.LoadEnv();

var builder = WebApplication.CreateBuilder(args);

ProgramSetup.ConfigureBuilder(builder);

var app = builder.Build();

await ProgramSetup.ConfigureApp(app).ConfigureAwait(false);

Console.WriteLine("[AdminService] About to start app.RunAsync()...");
app.Lifetime.ApplicationStarted.Register(() => Console.WriteLine("[AdminService] ApplicationStarted event fired — app is running."));

await app.RunAsync().ConfigureAwait(false);
