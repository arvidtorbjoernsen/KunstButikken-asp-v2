namespace KunstButikken.UserService.Application.Services;

using KunstButikken.UserService.Application.Interfaces;
using KunstButikken.UserService.Domain.Dtos;
using KunstButikken.UserService.Domain.Interfaces;

public sealed class SellerQueryService(IUserRepository repo) : ISellerQueryService
{
    public async Task<IReadOnlyList<SellerDto>> GetSellersAsync(CancellationToken ct = default)
    {
        return await repo.GetSellersAsync(ct).ConfigureAwait(false);
    }

    public async Task<SellerDto?> GetSellerByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await repo.GetSellerByUserIdAsync(userId, ct).ConfigureAwait(false);
    }
}
