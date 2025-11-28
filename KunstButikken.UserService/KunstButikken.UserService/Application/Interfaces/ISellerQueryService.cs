namespace KunstButikken.UserService.Application.Interfaces;

using KunstButikken.UserService.Domain.Dtos;

public interface ISellerQueryService
{
    Task<IReadOnlyList<SellerDto>> GetSellersAsync(CancellationToken ct = default);
    Task<SellerDto?> GetSellerByUserIdAsync(Guid userId, CancellationToken ct = default);
}

