namespace KunstButikken.PaymentService.Application.Interfaces;

public interface IPaymentService
{
    Task ProcessCheckoutAsync(object request, CancellationToken cancellationToken = default);
}

