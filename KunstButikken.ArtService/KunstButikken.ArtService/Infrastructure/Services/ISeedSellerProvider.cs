using KunstButikken.ArtService.Domain.ReadModels;

namespace KunstButikken.ArtService.Infrastructure.Services;

public interface ISeedSellerProvider
{
    Task<IReadOnlyList<SeedSeller>> GetSellersAsync(int maxCount, CancellationToken cancellationToken = default);
}

