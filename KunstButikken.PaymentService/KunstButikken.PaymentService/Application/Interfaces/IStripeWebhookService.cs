using Stripe;

namespace KunstButikken.PaymentService.Application.Interfaces;

public interface IStripeWebhookService
{
    Event ConstructEvent(string payload, string signature);
}
