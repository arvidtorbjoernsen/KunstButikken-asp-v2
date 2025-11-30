using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text;

using KunstButikken.ArtService.Application.Interfaces;
using KunstButikken.ArtService.Domain.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Updated namespace

namespace KunstButikken.ArtService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArtController(IArtService artService) : ControllerBase
{
    // Static mapping of admin actions to the corresponding modification lambda.
    // For Reject we leave the mapping null and handle it specially.
    private static readonly Dictionary<AdminAction, Action<Art>?> s_modifyActions =
        new()
        {
            [AdminAction.Publish] = art => art.Status = ArtStatus.Published,
            [AdminAction.Verify] = art =>
            {
                art.IsVerified = true;
                if (art.Status == ArtStatus.Draft)
                {
                    art.Status = ArtStatus.Published;
                }
            },
            [AdminAction.Reject] = null,
            [AdminAction.Feature] = art => art.IsFeatured = true,
            [AdminAction.Unfeature] = art => art.IsFeatured = false
        };

    private readonly IArtService _svc = artService ?? throw new ArgumentNullException(nameof(artService));

    // Public listing
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Art>>> GetAll([FromQuery] ArtStatus? status, [FromQuery] bool? featured)
    {
        var items = await _svc.GetAllAsync(status, featured).ConfigureAwait(false);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Art>> GetById(Guid id)
    {
        var art = await _svc.GetByIdAsync(id).ConfigureAwait(false);
        return art is null ? NotFound() : Ok(art);
    }

    // Create art - sellers only
    [HttpPost]
    [SuppressMessage("Usage", "CA1062:Validate arguments of public methods",
        Justification = "Controller validates body parameter and returns BadRequest if null")]
    public async Task<ActionResult<Art>> Create([FromBody] Art art)
    {
        if (art is null)
        {
            return BadRequest("Missing request body");
        }

        art.Id = Guid.NewGuid();
        art.Status = ArtStatus.Draft;

        // Try to populate seller information from authenticated user claims
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUid))
            {
                art.SellerId = parsedUid;
            }

            // Prefer the Name claim, fall back to Identity.Name
            var nameClaim = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name;
            art.SellerDisplayName = nameClaim ?? string.Empty;
        }
        catch
        {
            // Swallow - if no user context is present, leave seller fields as defaults
        }

        var created = await _svc.CreateAsync(art).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new
        {
            id = created.Id
        }, created);
    }

    // Update own art - sellers only
    [HttpPut("{id:guid}")]
    [SuppressMessage("Usage", "CA1062:Validate arguments of public methods",
        Justification = "Controller validates body parameter and returns BadRequest if null")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Art update)
    {
        if (update is null)
        {
            return BadRequest("Missing request body");
        }

        await _svc.UpdateAsync(id, update).ConfigureAwait(false);
        return NoContent();
    }

    // Upload image for art - sellers only
    [HttpPost("{id:guid}/upload")]
    public async Task<IActionResult> UploadImage(Guid id, [FromForm] IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is missing");
        }

        await using var stream = file.OpenReadStream();
        var resp = await _svc.UploadImageAsync(id, stream, file.FileName, file.ContentType).ConfigureAwait(false);
        return Ok(resp);
    }

    // Consolidated admin actions to reduce duplication: publish/verify/reject/feature/unfeature
    [HttpPost("{id:guid}/publish")]
    [HttpPost("{id:guid}/verify")]
    [HttpPost("{id:guid}/reject")]
    [HttpPost("{id:guid}/feature")]
    [HttpDelete("{id:guid}/feature")]
    public async Task<IActionResult> AdminModify(Guid id)
    {
        var action = DetermineAdminAction(HttpContext.Request.Path.Value ?? string.Empty,
            HttpContext.Request.Method.ToUpperInvariant());

        if (action == AdminAction.Reject)
        {
            return await HandleRejectAsync(id).ConfigureAwait(false);
        }

        if (!s_modifyActions.TryGetValue(action, out var modify) || modify is null)
        {
            return BadRequest("Unknown admin action");
        }

        // fetch entity, apply modification and persist via service
        var art = await _svc.GetByIdAsync(id).ConfigureAwait(false);
        if (art is null)
        {
            return NotFound();
        }

        modify(art);
        await _svc.UpdateAsync(id, art).ConfigureAwait(false);
        return NoContent();
    }

    // Determine the admin action based on request path and method
    internal static AdminAction DetermineAdminAction(string path, string method)
    {
        var segment = GetLastPathSegment(path);
        if (string.Equals(segment, "feature", StringComparison.OrdinalIgnoreCase))
        {
            return IsDeleteMethod(method) ? AdminAction.Unfeature : AdminAction.Feature;
        }

        return MapSegmentToAction(segment);
    }

    private static string GetLastPathSegment(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var parts = path.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        // Expecting paths like: /api/art/{id}/{action} -> parts = ["api","art","{id}","{action}"]
        if (parts.Length < 4)
        {
            return string.Empty;
        }

        return parts[^1].ToUpperInvariant();
    }

    private static bool IsDeleteMethod(string? method) =>
        string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase);

    private static AdminAction MapSegmentToAction(string segment)
    {
        return segment switch
        {
            "publish" => AdminAction.Publish,
            "verify" => AdminAction.Verify,
            "reject" => AdminAction.Reject,
            _ => AdminAction.Unknown
        };
    }

    // Handle reject action (reads optional body then updates art)
    private async Task<IActionResult> HandleRejectAsync(Guid id)
    {
        // Optional reason in body (ignored for now)
        try
        {
            // Leave the underlying Request.Body open when using StreamReader to avoid disposing it
            using var sr = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true);
            _ = await sr.ReadToEndAsync().ConfigureAwait(false);
        }
        catch
        {
            // ignore read failures and continue with reject behavior
        }

        return await ModifyArtAsync(id, art =>
        {
            art.IsVerified = false;
            art.Status = ArtStatus.Rejected;
        }).ConfigureAwait(false);
    }

    // Public: get featured list
    [HttpGet("featured")]
    public async Task<ActionResult<IEnumerable<Art>>> GetFeatured([FromQuery] int limit = 5)
    {
        if (limit <= 0)
        {
            limit = 5;
        }

        limit = Math.Min(limit, 20);
        var items = await _svc.GetFeaturedAsync(limit).ConfigureAwait(false);
        return Ok(items);
    }

    // Admin: list unverified artworks
    [HttpGet("unverified")]
    public async Task<ActionResult<IEnumerable<Art>>> GetUnverified()
    {
        var items = await _svc.GetUnverifiedAsync().ConfigureAwait(false);
        return Ok(items);
    }

    // Admin delete
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var art = await _svc.GetByIdAsync(id).ConfigureAwait(false);
        if (art is null)
        {
            return NotFound();
        }

        await _svc.DeleteAsync(id).ConfigureAwait(false);
        return NoContent();
    }

    // Helper to reduce duplication when admin modifies an art entity
    private async Task<IActionResult> ModifyArtAsync(Guid id, Action<Art> modify)
    {
        var art = await _svc.GetByIdAsync(id).ConfigureAwait(false);
        if (art is null)
        {
            return NotFound();
        }

        modify(art);
        await _svc.UpdateAsync(id, art).ConfigureAwait(false);
        return NoContent();
    }

    // Authenticated seller: get own art inventory
    [HttpGet("mine")]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<IEnumerable<Art>>> GetMine([FromQuery] bool includeUnverified = true, CancellationToken ct = default)
    {
        var sellerIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(sellerIdValue) || !Guid.TryParse(sellerIdValue, out var sellerId))
        {
            return Forbid();
        }

        var items = await _svc.GetBySellerAsync(sellerId, includeUnverified, ct).ConfigureAwait(false);
        return Ok(items);
    }

    // Helper enum representing admin actions
    internal enum AdminAction
    {
        Unknown,
        Publish,
        Verify,
        Reject,
        Feature,
        Unfeature
    }
}
