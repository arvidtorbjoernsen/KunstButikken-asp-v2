// #pragma warning disable CS8604,CS8601,CS8625
// Top-level program: compose resources and run

var builder = DistributedApplication.CreateBuilder(args);

// Apply Aspire defaults globally (logging, metrics, health checks, CORS)
builder.AddAspireDefaults();

// Compose the application resources (databases, services, frontends)
var composition = AppCompositionBuilder.Configure(builder);

// Try register MCP server services if available
AppHostHelpers.TryRegisterMcpServer(builder);

// Enable CORS for frontends and services
composition.NextJsFrontend.WithCors();
composition.AngularFrontend.WithCors();

// For microservices, allow CORS from frontends automatically
composition.AuthGateway.WithCors();
composition.UserService.WithCors();
composition.ArtService.WithCors();
composition.AuctionService.WithCors();
composition.PaymentService.WithCors();
composition.AdminService.WithCors();

// Prepare lists for PropagateCors
var frontends = new List<IResourceBuilder<IResourceWithEndpoints>> { composition.NextJsFrontend, composition.AngularFrontend };
var services = new List<IResourceBuilder<IResourceWithEnvironment>>
{
    composition.AuthGateway,
    composition.UserService,
    composition.ArtService,
    composition.AuctionService,
    composition.PaymentService,
    composition.AdminService
};

// Propagate CORS settings from frontends to microservices
AppHostHelpers.PropagateCors(frontends, services);

// Build and run
var app = builder.Build();
Console.WriteLine("[AppHost] AppHost starting...");
app.Run();

// #pragma warning restore CS8604,CS8601,CS8625
