using Stripe;
using Stripe.Checkout;

namespace KunstButikken.PaymentService.Services;

public class StripeSessionService : IStripeSessionService
{
    private readonly SessionService _sessionService = new();

    public Task<Session> CreateAsync(SessionCreateOptions options, RequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default) =>
        _sessionService.CreateAsync(options, requestOptions, cancellationToken);
}
