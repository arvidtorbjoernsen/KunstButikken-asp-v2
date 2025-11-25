using KunstButikken.PaymentService.Models;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.PaymentService.Data;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
  public DbSet<Transaction> Transactions => Set<Transaction>();
}