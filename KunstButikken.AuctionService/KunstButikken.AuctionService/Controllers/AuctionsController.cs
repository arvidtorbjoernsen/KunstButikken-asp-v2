using KunstButikken.AuctionService.Application.Interfaces;
using KunstButikken.AuctionService.Domain.Models;
using KunstButikken.AuctionService.Hubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace KunstButikken.AuctionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuctionsController : ControllerBase
{
    private readonly IHubContext<AuctionHub> _hub;
    private readonly IAuctionService _service;

    public AuctionsController(IAuctionService service, IHubContext<AuctionHub> hub)
    {
        _service = service;
        _hub = hub;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Auction>>> GetAll([FromQuery] AuctionStatus? status)
    {
        var auctions = await _service.GetAllAsync(status, includeBids: true).ConfigureAwait(false);
        return Ok(auctions);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Auction>> Get(Guid id)
    {
        var auction = await _service.GetByIdAsync(id, includeBids: true).ConfigureAwait(false);
        return auction is null ? NotFound() : Ok(auction);
    }

    [HttpPost]
    public async Task<ActionResult<Auction>> Create([FromBody] Auction auction)
    {
        if (auction is null)
        {
            throw new ArgumentNullException(nameof(auction));
        }

        var created = await _service.CreateAsync(auction).ConfigureAwait(false);
        await NotifyUpdatedAsync(created.Id, created).ConfigureAwait(false);
        await _hub.Clients.All.SendAsync("auctionCreated", created).ConfigureAwait(false);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] Auction updatedAuction)
    {
        if (updatedAuction is null)
        {
            throw new ArgumentNullException(nameof(updatedAuction));
        }

        var userId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        Guid? sellerId = Guid.TryParse(userId, out var parsed) ? parsed : null;
        try
        {
            var auction = await _service.UpdateAsync(id, updatedAuction, sellerId).ConfigureAwait(false);
            if (auction is null) return NotFound();
            await NotifyUpdatedAsync(id, auction).ConfigureAwait(false);
            return Ok(auction);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:guid}/bid")]
    public async Task<IActionResult> Bid(Guid id, [FromBody] decimal amount)
    {
        try
        {
            var updated = await _service.PlaceBidAsync(id, amount, Guid.NewGuid()).ConfigureAwait(false);
            if (updated is null) return NotFound();
            await NotifyUpdatedAsync(id, updated).ConfigureAwait(false);
            return Accepted();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id)
    {
        var auction = await _service.CloseAsync(id).ConfigureAwait(false);
        if (auction is null) return NotFound();
        await _hub.Clients.Group(id.ToString()).SendAsync("auctionClosed", auction).ConfigureAwait(false);
        await NotifyUpdatedAsync(id, auction).ConfigureAwait(false);
        return NoContent();
    }

    private Task NotifyUpdatedAsync(Guid auctionId, Auction auction) => _hub.Clients.Group(auctionId.ToString()).SendAsync("auctionUpdated", auction);
}
