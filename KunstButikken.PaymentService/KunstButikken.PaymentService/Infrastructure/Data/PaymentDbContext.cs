using KunstButikken.PaymentService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KunstButikken.PaymentService.Infrastructure.Data;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
}
