using Stripe;

namespace KunstButikken.PaymentService.Services;

public class StripeWebhookService : IStripeWebhookService
{
    public Event ConstructEvent(string json, string signature, string secret) =>
        EventUtility.ConstructEvent(json, signature, secret);
}
