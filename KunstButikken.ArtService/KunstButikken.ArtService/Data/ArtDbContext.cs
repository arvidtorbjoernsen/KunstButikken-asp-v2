using Microsoft.EntityFrameworkCore;

namespace KunstButikken.ArtService.Data;

public class ArtDbContext : Infrastructure.Data.ArtDbContext
{
    public ArtDbContext(DbContextOptions<Infrastructure.Data.ArtDbContext> options)
        : base(options)
    {
    }
}
