using KunstButikken.AdminService.Data;
using KunstButikken.Common.Logging;
using Microsoft.AspNetCore.Mvc;

namespace KunstButikken.AdminService.Controllers;

[ApiController]
[Route("health")]
public class ReadinessController(IServiceProvider services, IConfiguration config, ILogger<ReadinessController> logger)
    : ControllerBase
{
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        var result = new Dictionary<string, object?>();
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetService<AdminDbContext>();
            if (db == null)
            {
                result["db"] = "missing-service";
            }
            else
            {
                result["db"] = await db.Database.CanConnectAsync(ct).ConfigureAwait(false);
            }

            // Reference 'config' so it is not considered unused by the compiler/analyzers
            var version = config["SERVICE_VERSION"];
            if (!string.IsNullOrEmpty(version))
            {
                result["version"] = version;
            }
        }
        catch (Exception ex)
        {
            LogMessages.Warning_Msg_31(logger, ex);
            result["db"] = ex.Message;
        }

        var ok = result.Values.All(v => v is bool b && b) && result.Count > 0;
        return ok ? Ok(result) : StatusCode(503, result);
    }
}
