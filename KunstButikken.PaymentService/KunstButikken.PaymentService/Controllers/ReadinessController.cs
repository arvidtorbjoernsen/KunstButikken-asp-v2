using KunstButikken.Common.Logging;
using KunstButikken.PaymentService.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace KunstButikken.PaymentService.Controllers;

[ApiController]
[Route("health")]
public class ReadinessController(IServiceProvider services, ILogger<ReadinessController> logger)
    : ControllerBase
{
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        var result = new Dictionary<string, object?>();
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetService<PaymentDbContext>();
            if (db == null)
            {
                result["db"] = "missing-service";
            }
            else
            {
                result["db"] = await db.Database.CanConnectAsync(ct).ConfigureAwait(false);
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
