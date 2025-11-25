using KunstButikken.PaymentService.Models;

namespace KunstButikken.PaymentService.Data;

public class PaymentRepository(PaymentDbContext db) : IPaymentRepository
{
    public IQueryable<Transaction> Query() =>
        db.Transactions.AsQueryable();

    public Task<Transaction?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Transactions.FindAsync([id], cancellationToken).AsTask();

    public Task AddAsync(Transaction payment, CancellationToken cancellationToken = default)
    {
        db.Transactions.Add(payment);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
