using KunstButikken.ArtService.Infrastructure.Services;
using KunstButikken.ArtService.Domain.ReadModels;
using KunstButikken.ArtService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.ArtService.Infrastructure.Services;

internal sealed class SeedSellerProvider(SeedSellerDbContext dbContext) : ISeedSellerProvider
{
    public async Task<IReadOnlyList<SeedSeller>> GetSellersAsync(int maxCount, CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(maxCount, 1, 50);
        return await dbContext.Profiles
            .Where(p => p.IsSeller && p.IsSellerVerified)
            .OrderBy(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new SeedSeller(p.UserId, p.DisplayName))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
