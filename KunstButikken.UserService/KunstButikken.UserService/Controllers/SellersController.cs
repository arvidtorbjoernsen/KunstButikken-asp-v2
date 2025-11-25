using KunstButikken.UserService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SellersController(IUserRepository repo) : ControllerBase
{
    /// <summary>
    ///     Get all verified sellers. Used by other services for seeding and lookups.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SellerDto>>> GetSellers()
    {
        var sellers = await repo.Query()
            .Where(p => p.IsSeller)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new SellerDto
            {
                Id = p.Id, UserId = p.UserId, DisplayName = p.DisplayName, Email = p.Email
            })
            .ToListAsync().ConfigureAwait(false);

        return Ok(sellers);
    }

    /// <summary>
    ///     Get a specific seller by UserId (Keycloak user ID)
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<SellerDto>> GetSellerByUserId(Guid userId)
    {
        var seller = await repo.Query()
            .Where(p => p.UserId == userId && p.IsSeller)
            .Select(p => new SellerDto
            {
                Id = p.Id, UserId = p.UserId, DisplayName = p.DisplayName, Email = p.Email
            })
            .FirstOrDefaultAsync().ConfigureAwait(false);

        if (seller == null)
        {
            return NotFound(new
            {
                message = $"Seller with UserId {userId} not found"
            });
        }

        return Ok(seller);
    }
}

public class SellerDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
