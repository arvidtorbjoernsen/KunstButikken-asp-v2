namespace KunstButikken.UserService.Infrastructure.Repositories;

using KunstButikken.UserService.Domain.Dtos;
using KunstButikken.UserService.Domain.Entities;
using KunstButikken.UserService.Domain.Interfaces;
using KunstButikken.UserService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class UserRepository : IUserRepository
{
    private readonly UserDbContext _db;

    public UserRepository(UserDbContext db)
    {
        _db = db;
    }

    public async Task<UserProfile?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Profiles.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);

    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, ct).ConfigureAwait(false);

    public async Task<UserProfile?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await _db.Profiles.FirstOrDefaultAsync(p => p.Email == email, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<UserProfile>> GetPendingSellersAsync(CancellationToken ct = default) =>
        await _db.Profiles
            .Where(p => p.IsSeller && !p.IsSellerVerified)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<UserProfile>> GetAdminsAsync(CancellationToken ct = default) =>
        await _db.Profiles
            .Where(p => p.IsAdmin)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<UnverifiedSellerDto>> ListUnverifiedSellersAsync(CancellationToken ct = default) =>
        await _db.Profiles
            .Where(p => p.IsSeller && !p.IsSellerVerified)
            .Select(p => new UnverifiedSellerDto
            {
                Id = p.Id,
                UserId = p.UserId,
                DisplayName = p.DisplayName,
                Email = p.Email
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<SellerDto>> GetSellersAsync(CancellationToken ct = default) =>
        await _db.Profiles
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

    public async Task<SellerDto?> GetSellerByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _db.Profiles
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

    public async Task AddAsync(UserProfile user, CancellationToken cancellationToken = default)
    {
        await _db.Profiles.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(UserProfile user, CancellationToken cancellationToken = default)
    {
        _db.Profiles.Update(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
