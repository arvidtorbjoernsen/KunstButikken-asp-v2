using System.Text.Json;
using KunstButikken.UserService.Application.Interfaces;
using KunstButikken.UserService.Shared.Dev;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.UserService.Controllers;

[ApiController]
[Route("api/dev")]
public class DevController(IDevSeedService devSeedService, IKeycloakSyncService keycloakSyncService) : ControllerBase
{
    [HttpPost("sync-keycloak-users")]
    public async Task<ActionResult<SyncUsersResult>> SyncKeycloakUsers(CancellationToken cancellationToken)
    {
        var result = await keycloakSyncService.TriggerSyncAsync(cancellationToken).ConfigureAwait(false);
        return result.Allowed ? Ok(result) : Forbid();
    }

    [HttpPost("seed-keycloak-users")]
    public async Task<ActionResult<SeedUsersResult>> SeedKeycloakUsers([FromBody] SeedUsersRequest? request, CancellationToken cancellationToken)
    {
        if (!devSeedService.IsAllowed())
        {
            return Forbid();
        }

        var result = await devSeedService.SeedKeycloakUsersAsync(request, cancellationToken).ConfigureAwait(false);
        return result.Allowed ? Ok(result) : Forbid();
    }
}
