using KunstButikken.ArtService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.ArtService.Infrastructure.Data;

public class ArtDbContext(DbContextOptions<ArtDbContext> options) : DbContext(options)
{
    public DbSet<Art> Arts => Set<Art>();
}
