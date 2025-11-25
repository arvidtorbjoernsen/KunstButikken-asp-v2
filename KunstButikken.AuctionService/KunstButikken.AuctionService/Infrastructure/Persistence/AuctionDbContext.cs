using System;
using System.Threading;
using System.Threading.Tasks;
using KunstButikken.AuctionService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.AuctionService.Infrastructure.Persistence;

public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options) { }

    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<Art> Arts => Set<Art>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (modelBuilder is null)
        {
            throw new ArgumentNullException(nameof(modelBuilder));
        }

        modelBuilder.Entity<Auction>().HasMany(a => a.Bids).WithOne().HasForeignKey(b => b.AuctionId);
        modelBuilder.Entity<Auction>()
            .Property(a => a.SellerDisplayName)
            .IsRequired()
            .HasDefaultValue(string.Empty);

        base.OnModelCreating(modelBuilder);
    }
}
