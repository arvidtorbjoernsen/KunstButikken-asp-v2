using KunstButikken.UserService;
using KunstButikken.ServiceDefaults;

// Load .env when running the service directly
EnvLoader.LoadEnv();

var builder = WebApplication.CreateBuilder(args);

// Delegate setup to ProgramSetup
ProgramSetup.ConfigureBuilder(builder);

var app = builder.Build();

// Configure the app and run
await ProgramSetup.ConfigureApp(app).ConfigureAwait(false);

// Add startup logger for UserService
Console.WriteLine("[UserService] About to start app.RunAsync()...");
app.Lifetime.ApplicationStarted.Register(() => Console.WriteLine("[UserService] ApplicationStarted event fired — app is running."));

await app.RunAsync().ConfigureAwait(false);

// OpenAPI document transformer to add Bearer token authentication
// internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
//   : IOpenApiDocumentTransformer
// {
//   public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
//     CancellationToken cancellationToken)
//   {
//     var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync().ConfigureAwait(false);
//     if (authenticationSchemes.Any(authScheme => authScheme.Name == JwtBearerDefaults.AuthenticationScheme))
//     {
//       var securityScheme = new OpenApiSecurityScheme
//       {
//         Type = SecuritySchemeType.Http,
//         Scheme = JwtBearerDefaults.AuthenticationScheme.ToLowerInvariant(),
//         In = ParameterLocation.Header,
//         BearerFormat = "JWT",
//         Description = "JWT Authorization header using the Bearer scheme."
//       };
//
//       document.Components ??= new OpenApiComponents();
//       document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
//       document.Components.SecuritySchemes["Bearer"] = securityScheme;
//
//       var securityRequirement = new OpenApiSecurityRequirement
//       {
//         [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
//       };
//
//       if (document.Paths != null)
//         foreach (var path in document.Paths.Values)
//           if (path.Operations != null)
//             foreach (var operation in path.Operations.Values)
//               operation.Security?.Add(securityRequirement);
//     }
//   }
// }
