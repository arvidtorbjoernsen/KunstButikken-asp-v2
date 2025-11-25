using KunstButikken.Common.Logging;
using Microsoft.AspNetCore.Mvc;

namespace KunstButikken.AuthGateway.Controllers;

[ApiController]
[Route("health")]
public class ReadinessController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<ReadinessController> _logger;

    public ReadinessController(IHttpClientFactory httpFactory, IConfiguration config,
        ILogger<ReadinessController> logger)
    {
        _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        var result = new Dictionary<string, object?>();

        // Check backend services
        var urls = new[]
        {
            "USER_SERVICE_URL", "ART_SERVICE_URL", "AUCTIONS_SERVICE_URL", "PAYMENT_SERVICE_URL", "ADMIN_SERVICE_URL"
        };

        using var client = _httpFactory.CreateClient();

        foreach (var key in urls)
        {
            try
            {
                var url = _config[key];
                if (string.IsNullOrWhiteSpace(url))
                {
                    result[key] = "missing-config";
                    continue;
                }

                var readyUri = new Uri(url.TrimEnd('/') + "/health/ready");
                using var res = await client.GetAsync(readyUri, ct).ConfigureAwait(false);
                result[key] = res.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                LogMessages.Warning_Key(_logger, key, ex);
                result[key] = ex.Message;
            }
        }

        var ok = result.Values.All(v => v is bool b && b) && result.Count > 0;
        return ok ? Ok(result) : StatusCode(503, result);
    }
}
