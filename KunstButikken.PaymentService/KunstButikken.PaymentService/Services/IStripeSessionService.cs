using Stripe;
using Stripe.Checkout;

// Added this using statement

namespace KunstButikken.PaymentService.Services;

public interface IStripeSessionService
{
  Task<Session> CreateAsync(SessionCreateOptions options, RequestOptions? requestOptions = null,
    CancellationToken cancellationToken = default);
}