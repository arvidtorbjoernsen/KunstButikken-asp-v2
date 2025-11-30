using KunstButikken.UserService.Application.Interfaces;
using KunstButikken.UserService.Domain.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace KunstButikken.UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SellersController(ISellerQueryService sellers) : ControllerBase
{
    /// <summary>
    ///     Get all verified sellers. Used by other services for seeding and lookups.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SellerDto>>> GetSellers()
    {
        var list = await sellers.GetSellersAsync().ConfigureAwait(false);
        return Ok(list);
    }

    /// <summary>
    ///     Get a specific seller by UserId (Keycloak user ID)
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<SellerDto>> GetSellerByUserId(Guid userId)
    {
        var seller = await sellers.GetSellerByUserIdAsync(userId).ConfigureAwait(false);

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
