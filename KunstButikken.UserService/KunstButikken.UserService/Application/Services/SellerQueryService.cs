namespace KunstButikken.UserService.Application.Services;

using KunstButikken.UserService.Application.Interfaces;
using KunstButikken.UserService.Domain.Dtos;
using KunstButikken.UserService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public sealed class SellerQueryService(IUserRepository repo) : ISellerQueryService
{
    public async Task<IReadOnlyList<SellerDto>> GetSellersAsync(CancellationToken ct = default)
    {
        return await repo.Query()
            .Where(p => p.IsSeller)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new SellerDto
            {
                Id = p.Id,
                UserId = p.UserId,
                DisplayName = p.DisplayName,
                Email = p.Email
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<SellerDto?> GetSellerByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await repo.Query()
            .Where(p => p.UserId == userId && p.IsSeller)
            .Select(p => new SellerDto
            {
                Id = p.Id,
                UserId = p.UserId,
                DisplayName = p.DisplayName,
                Email = p.Email
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}

