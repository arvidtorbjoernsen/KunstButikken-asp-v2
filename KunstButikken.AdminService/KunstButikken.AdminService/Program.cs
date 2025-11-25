using KunstButikken.AdminService;
using KunstButikken.AdminService.Application.Interfaces;
using KunstButikken.AdminService.Application.Services;
using KunstButikken.AdminService.Domain.Interfaces;
using KunstButikken.AdminService.Infrastructure.Repositories;
using KunstButikken.ServiceDefaults;

// Load .env when running the service directly
EnvLoader.LoadEnv();

var builder = WebApplication.CreateBuilder(args);

// Delegate builder configuration to helper to make it testable
ProgramSetup.ConfigureBuilder(builder);

// Register repository and application service
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();

var app = builder.Build();

// Delegate app configuration to helper (now testable)
await ProgramSetup.ConfigureApp(app).ConfigureAwait(false);

// Add startup logger for AdminService
Console.WriteLine("[AdminService] About to start app.RunAsync()...");
app.Lifetime.ApplicationStarted.Register(() => Console.WriteLine("[AdminService] ApplicationStarted event fired — app is running."));

await app.RunAsync().ConfigureAwait(false);
