using KunstButikken.AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.AdminService.Data;

public class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
  public DbSet<AdminLog> Logs => Set<AdminLog>();
}