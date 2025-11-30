using KunstButikken.PaymentService.Domain.Models;

namespace KunstButikken.PaymentService.Domain.Repositories;

public interface IPaymentRepository
{
    IQueryable<Transaction> Query();
    Task<Transaction?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction payment, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

