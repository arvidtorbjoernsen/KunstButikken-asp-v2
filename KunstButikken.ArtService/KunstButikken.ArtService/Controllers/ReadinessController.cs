using Microsoft.AspNetCore.Mvc;

namespace KunstButikken.ArtService.Controllers;

[ApiController]
[Route("/health/readiness")]
public class ReadinessController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;

    public ReadinessController(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
    }

    [HttpGet]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        try
        {
            using var client = _httpFactory.CreateClient();
            var uri = client.BaseAddress != null
                ? new Uri(client.BaseAddress, "/health/ready")
                : new Uri("/health/ready", UriKind.Relative);

            using var resp = await client.GetAsync(uri, cancellationToken).ConfigureAwait(false);
            return resp.IsSuccessStatusCode ? Ok() : StatusCode((int)resp.StatusCode);
        }
        catch
        {
            return StatusCode(503);
        }
    }
}
