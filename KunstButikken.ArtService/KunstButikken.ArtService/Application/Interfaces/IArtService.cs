using KunstButikken.ArtService.Domain.Models;
using KunstButikken.ArtService.Domain.DTOs;

namespace KunstButikken.ArtService.Application.Interfaces;

public interface IArtService
{
    Task<IEnumerable<Art>> GetAllAsync(ArtStatus? status = null, bool? featured = null, CancellationToken ct = default);
    Task<Art?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Art> CreateAsync(Art art, CancellationToken ct = default);
    Task UpdateAsync(Guid id, Art update, CancellationToken ct = default);
    Task<ImageUploadResponse> UploadImageAsync(Guid id, Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Art>> GetFeaturedAsync(int limit, CancellationToken ct = default);
    Task<IEnumerable<Art>> GetUnverifiedAsync(CancellationToken ct = default);
    Task<IEnumerable<Art>> GetBySellerAsync(Guid sellerId, bool includeUnverified = true, CancellationToken ct = default);
}
