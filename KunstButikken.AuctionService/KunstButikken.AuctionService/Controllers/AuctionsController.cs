using KunstButikken.AuctionService.Application.Interfaces;
using KunstButikken.AuctionService.Domain.Models;
using KunstButikken.AuctionService.Hubs;
using KunstButikken.ServiceDefaults;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.AuctionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuctionsController : ControllerBase
{
    private readonly IDateTimeProvider _clock;
    private readonly IHubContext<AuctionHub> _hub;
    private readonly IAuctionService _service;

    public AuctionsController(IAuctionService service, IHubContext<AuctionHub> hub, IDateTimeProvider clock)
    {
        _service = service;
        _hub = hub;
        _clock = clock;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Auction>>> GetAll([FromQuery] AuctionStatus? status)
    {
        IQueryable<Auction> q = _service.Query().Include(a => a.Bids);
        if (status.HasValue)
        {
            q = q.Where(a => a.Status == status);
        }

        return Ok(await q.OrderByDescending(a => a.StartsAt).ToListAsync().ConfigureAwait(false));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Auction>> Get(Guid id)
    {
        var a = await _service.Query().Include(x => x.Bids).FirstOrDefaultAsync(x => x.Id == id).ConfigureAwait(false);
        return a is null ? NotFound() : Ok(a);
    }

    [HttpPost]
    public async Task<ActionResult<Auction>> Create([FromBody] Auction a)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }

        a.Id = Guid.NewGuid();
        a.Status = AuctionStatus.Open;
        await _service.AddAsync(a).ConfigureAwait(false);
        await _service.SaveChangesAsync().ConfigureAwait(false);
        await _hub.Clients.Group(a.Id.ToString()).SendAsync("auctionUpdated", a).ConfigureAwait(false);
        await _hub.Clients.All.SendAsync("auctionCreated", a).ConfigureAwait(false);
        return CreatedAtAction(nameof(Get), new
        {
            id = a.Id
        }, a);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] Auction updatedAuction)
    {
        if (updatedAuction is null)
        {
            throw new ArgumentNullException(nameof(updatedAuction));
        }

        var auction = await _service.Query().Include(x => x.Bids).FirstOrDefaultAsync(x => x.Id == id).ConfigureAwait(false);
        if (auction is null)
        {
            return NotFound();
        }

        var userId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (auction.SellerId.ToString() != userId)
        {
            return Forbid();
        }

        if (auction.Bids.Count > 0)
        {
            return BadRequest("Cannot edit an auction with bids.");
        }

        auction.StartsAt = updatedAuction.StartsAt;
        auction.EndsAt = updatedAuction.EndsAt;
        auction.StartingPrice = updatedAuction.StartingPrice;
        auction.ReservePrice = updatedAuction.ReservePrice;

        await _service.SaveChangesAsync().ConfigureAwait(false);

        var freshAuction = await _service.Query().Include(x => x.Bids).FirstOrDefaultAsync(x => x.Id == id).ConfigureAwait(false);
        await _hub.Clients.Group(id.ToString()).SendAsync("auctionUpdated", freshAuction).ConfigureAwait(false);

        return Ok(freshAuction);
    }

    [HttpPost("{id:guid}/bid")]
    public async Task<IActionResult> Bid(Guid id, [FromBody] decimal amount)
    {
        var a = await _service.Query().Include(x => x.Bids).FirstOrDefaultAsync(x => x.Id == id).ConfigureAwait(false);
        if (a is null)
        {
            return NotFound();
        }

        if (a.Status != AuctionStatus.Open || a.EndsAt <= _clock.UtcNow)
        {
            return BadRequest("Auction closed");
        }

        var min = a.Bids.Count == 0 ? a.StartingPrice : a.Bids.Max(b => b.Amount);
        if (amount <= min)
        {
            return BadRequest("Bid must be greater than " + min);
        }

        var bid = new Bid
        {
            Id = Guid.NewGuid(), AuctionId = id, BidderId = Guid.NewGuid(), Amount = amount
        };
        await _service.AddBidAsync(bid).ConfigureAwait(false);
        await _service.SaveChangesAsync().ConfigureAwait(false);

        var updatedAuction = await _service.Query().Include(x => x.Bids).FirstOrDefaultAsync(x => x.Id == id).ConfigureAwait(false);

        await _hub.Clients.Group(id.ToString()).SendAsync("auctionUpdated", updatedAuction).ConfigureAwait(false);
        return Accepted();
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id)
    {
        var a = await _service.Query().Include(x => x.Bids).FirstOrDefaultAsync(x => x.Id == id).ConfigureAwait(false);
        if (a is null)
        {
            return NotFound();
        }

        a.Status = AuctionStatus.Closed;
        var top = a.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
        var reserveOk = !a.ReservePrice.HasValue || top != null && top.Amount >= a.ReservePrice.Value;
        if (top != null && reserveOk)
        {
            a.WinningBid = top.Amount;
            a.WinnerId = top.BidderId;
        }
        else
        {
            a.WinningBid = null;
            a.WinnerId = null;
        }

        await _service.SaveChangesAsync().ConfigureAwait(false);
        await _hub.Clients.Group(id.ToString()).SendAsync("auctionClosed", a).ConfigureAwait(false);
        await _hub.Clients.Group(id.ToString()).SendAsync("auctionUpdated", a).ConfigureAwait(false);
        return NoContent();
    }
}
