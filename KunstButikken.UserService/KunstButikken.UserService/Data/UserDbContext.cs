using KunstButikken.UserService.Models;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.UserService.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<UserProfile> Profiles => Set<UserProfile>();
}
