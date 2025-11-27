using KunstButikken.PaymentService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace KunstButikken.PaymentService.Application.Services;

public class StripeWebhookService : IStripeWebhookService
{
    private readonly IConfiguration _configuration;

    public StripeWebhookService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Event ConstructEvent(string payload, string signature)
    {
        var secret = _configuration["Stripe:WebhookSecret"] ?? _configuration["Stripe__WebhookSecret"] ?? throw new InvalidOperationException("Stripe webhook secret missing");
        return EventUtility.ConstructEvent(payload, signature, secret);
    }
}
