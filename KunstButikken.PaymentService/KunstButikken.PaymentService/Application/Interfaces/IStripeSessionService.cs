using Stripe;
using Stripe.Checkout;

namespace KunstButikken.PaymentService.Application.Interfaces;

public interface IStripeSessionService
{
    Task<Session> CreateAsync(SessionCreateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default);
}
