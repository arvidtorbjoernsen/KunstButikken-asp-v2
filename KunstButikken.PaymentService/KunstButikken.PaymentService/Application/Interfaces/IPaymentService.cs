using KunstButikken.PaymentService.Application.Models;

namespace KunstButikken.PaymentService.Application.Interfaces;

public interface IPaymentService
{
    Task<SessionResponse> ProcessCheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default);
}
