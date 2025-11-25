// This file previously contained a derived ArtDbContext that forwarded to Infrastructure.Data.ArtDbContext.
// The project has been reorganized and the concrete ArtDbContext implementation now lives in
// Infrastructure/Data/ArtDbContext.cs with namespace KunstButikken.ArtService.Data.
// The duplicate forwarding type was removed to avoid ambiguity and keep a single source-of-truth.

using Microsoft.EntityFrameworkCore;
using KunstButikken.ArtService.Infrastructure.Data;

namespace KunstButikken.ArtService.Data;

// Forwarding DbContext to preserve existing references to KunstButikken.ArtService.Data.ArtDbContext
// The real implementation lives under Infrastructure.Data.ArtDbContext; this type simply forwards
// the constructor so code and generated migrations that reference the Data namespace continue to compile.
public class ArtDbContext : KunstButikken.ArtService.Infrastructure.Data.ArtDbContext
{
    public ArtDbContext(DbContextOptions<KunstButikken.ArtService.Infrastructure.Data.ArtDbContext> options)
        : base(options)
    {
    }
}
