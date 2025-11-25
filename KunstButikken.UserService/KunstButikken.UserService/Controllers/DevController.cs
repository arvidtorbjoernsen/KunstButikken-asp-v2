using System.Text.Json;
using KunstButikken.UserService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Added for DbUpdateException

namespace KunstButikken.UserService.Controllers;

[ApiController]
[Route("api/dev")] // dev-only utilities
public class DevController(
    IConfiguration cfg,
    IHttpClientFactory http,
    IWebHostEnvironment env,
    IServiceProvider services,
    ILogger<DevController>? logger = null) : ControllerBase
{ // Reference constructor parameters to avoid CS9113 "parameter is unread" warnings
    private readonly IHttpClientFactory _httpFactory = http;
    private readonly ILogger<DevController>? _loggerField = logger;

    private static readonly string[] RolesSeller = ["seller"];
    private static readonly string[] RolesBuyer = ["buyer"];
    private static readonly string[] RolesAdmin = ["admin"];

    private static readonly (string Username, string[] Roles)[] DefaultDesiredUsers =
    [
        ("seller1", RolesSeller),
        ("seller2", RolesSeller),
        ("seller3", RolesSeller),
        ("buyer1", RolesBuyer),
        ("buyer2", RolesBuyer),
        ("buyer3", RolesBuyer),
        ("buyer4", RolesBuyer),
        ("buyer5", RolesBuyer),
        ("buyer6", RolesBuyer),
        ("buyer7", RolesBuyer),
        ("app-admin", RolesAdmin)
    ];

    [HttpPost("sync-keycloak-users")]
    public async Task<ActionResult<SyncUsersResult>> SyncKeycloakUsers()
    {
        var allow = env.IsDevelopment() ||
                    string.Equals(cfg["ALLOW_DEV_SEED"], "true", StringComparison.OrdinalIgnoreCase);
        var res = new SyncUsersResult { Allowed = allow };
        if (!allow)
        {
            return Forbid();
        }

        try
        {
            var svc = services.GetService<KeycloakSyncService>();
            if (svc == null)
            {
                res.LastRunError = "KeycloakSyncService is not registered";
                return StatusCode(500, res);
            }

            await svc.TriggerRunAsync().ConfigureAwait(false);
            res.Triggered = true;
            res.LastRunError = svc.LastRunError;
            res.LastRunSucceeded = svc.LastRunSucceeded;
            res.LastRunUtc = svc.LastRunUtc;
            res.LastRunDuration = svc.LastRunDuration;
            // Include diagnostic fields
            res.LastAuthTokenEndpoint = svc.LastAuthTokenEndpoint;
            res.LastAuthTokenRealm = svc.LastAuthTokenRealm;
            res.LastAuthClientId = svc.LastAuthClientId;
            res.LastAuthGrant = svc.LastAuthGrant;
            res.LastAuthHttpError = svc.LastAuthHttpError;
            return Ok(res);
        }
        catch (OperationCanceledException ex)
        {
            res.LastRunError = ex.Message;
            return StatusCode(500, res);
        }
        catch (HttpRequestException ex)
        {
            res.LastRunError = ex.Message;
            return StatusCode(500, res);
        }
        catch (JsonException ex)
        {
            res.LastRunError = ex.Message;
            return StatusCode(500, res);
        }
        catch (DbUpdateException ex)
        {
            res.LastRunError = ex.Message;
            return StatusCode(500, res);
        }
    }

    [HttpPost("seed-keycloak-users")]
    public async Task<ActionResult<SeedUsersResult>> SeedKeycloakUsers([FromBody] SeedUsersRequest? req)
    {
        var svc = services.GetService<DevSeedService>();
        if (svc == null)
        {
            return StatusCode(500,
                new SeedUsersResult { Allowed = false, Errors = { "DevSeedService not registered" } });
        }

        if (!svc.IsAllowed())
        {
            return Forbid();
        }

        var result = await svc.SeedKeycloakUsersAsync(req).ConfigureAwait(false);
        return result.Allowed ? Ok(result) : Forbid();
    }
}
