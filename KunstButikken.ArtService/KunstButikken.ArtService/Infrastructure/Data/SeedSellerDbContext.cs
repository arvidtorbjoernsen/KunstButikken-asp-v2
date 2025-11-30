using KunstButikken.ArtService.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.ArtService.Infrastructure.Data;

internal sealed class SeedSellerDbContext(DbContextOptions<SeedSellerDbContext> options) : DbContext(options)
{
    internal DbSet<UserProfileSnapshot> Profiles => Set<UserProfileSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfileSnapshot>(entity =>
        {
            entity.ToTable("Profiles");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.DisplayName).HasMaxLength(256);
            entity.Property(x => x.Email).HasMaxLength(256);
        });
    }
}
