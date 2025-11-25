using KunstButikken.AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.AdminService.Infrastructure.Data;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<AdminLog> Logs => Set<AdminLog>();
}
