using Stripe;

namespace KunstButikken.PaymentService.Services;

public interface IStripeWebhookService
{
  Event ConstructEvent(string json, string signature, string secret);
}