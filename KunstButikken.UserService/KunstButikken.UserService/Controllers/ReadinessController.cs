using System.Data.Common;
using KunstButikken.Common.Logging;
using KunstButikken.UserService.Data;
using Microsoft.AspNetCore.Mvc;

namespace KunstButikken.UserService.Controllers;

[ApiController]
[Route("health")]
public class ReadinessController(
    IServiceProvider services,
    IHttpClientFactory httpFactory,
    IConfiguration config,
    ILogger<ReadinessController> logger)
    : ControllerBase
{
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        var result = new Dictionary<string, object?>();

        // DB check
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetService<UserDbContext>();
            if (db == null)
            {
                result["db"] = "missing-service";
            }
            else
            {
                var canConnect = await db.Database.CanConnectAsync(ct).ConfigureAwait(false);
                result["db"] = canConnect;
            }
        }
        catch (DbException ex) // General EF Core database exception
        {
            logger.LogWarning(ex, "Readiness check: DB operation failed (DB Exception)");
            result["db"] = ex.Message;
        }
        // Removed catch (Exception ex) to address CA1031

        // Keycloak check (optional)
        try
        {
            var issuer = config["KEYCLOAK_ISSUER"] ?? config["NEXT_PUBLIC_KEYCLOAK_ISSUER"];
            if (string.IsNullOrWhiteSpace(issuer))
            {
                result["keycloak"] = "missing-config";
            }
            else
            {
                using var client = httpFactory.CreateClient();
                var metadata = issuer.TrimEnd('/') + "/.well-known/openid-configuration";
                var metadataUri = new Uri(metadata);
                using var res = await client.GetAsync(metadataUri, ct).ConfigureAwait(false);
                result["keycloak"] = res.IsSuccessStatusCode;
            }
        }
        catch (HttpRequestException ex) // More specific for HTTP requests
        {
            LogMessages.Warning_Msg_5(logger, ex);
            result["keycloak"] = ex.Message;
        }
        // Removed catch (Exception ex) to address CA1031

        // Return 200 only if all boolean checks are true
        var ok = result.Values.All(v => v is bool b && b) && result.Count > 0;
        return ok ? Ok(result) : StatusCode(503, result);
    }
}
