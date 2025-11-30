using KunstButikken.PaymentService.Application.Interfaces;
using KunstButikken.PaymentService.Application.Models;
using KunstButikken.PaymentService.Domain.Models;
using KunstButikken.PaymentService.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace KunstButikken.PaymentService.Application.Services;

public sealed class PaymentOrchestrationService : IPaymentService
{
    private readonly IPaymentRepository _repository;
    private readonly IStripeSessionService _sessionService;
    private readonly IConfiguration _configuration;

    public PaymentOrchestrationService(
        IPaymentRepository repository,
        IStripeSessionService sessionService,
        IConfiguration configuration)
    {
        _repository = repository;
        _sessionService = sessionService;
        _configuration = configuration;
    }

    public async Task<SessionResponse> ProcessCheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Currency))
        {
            request.Currency = "usd";
        }

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            ArtId = request.ArtId,
            AuctionId = request.AuctionId,
            Amount = request.Amount,
            Currency = request.Currency,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _repository.AddAsync(transaction, cancellationToken).ConfigureAwait(false);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var apiKey = ResolveStripeApiKey();
        StripeConfiguration.ApiKey = apiKey;

        var sessionOptions = BuildSessionOptions(transaction, request);
        var session = await _sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

        transaction.StripeSessionId = session.Id;
        transaction.Status = TransactionStatus.Pending;
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SessionResponse
        {
            SessionId = session.Id,
            Url = session.Url,
            TransactionId = transaction.Id
        };
    }

    private string ResolveStripeApiKey()
    {
        var apiKey = _configuration["Stripe:ApiKey"] ?? _configuration["Stripe__ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Stripe API key is not configured. Set Stripe:ApiKey or Stripe__ApiKey.");
        }

        return apiKey;
    }

    private static SessionCreateOptions BuildSessionOptions(Transaction transaction, CheckoutRequest request)
    {
        var baseUrl = request.FrontendBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "https://kunstbutikken.example";
        }
        if (baseUrl.EndsWith('/'))
        {
            baseUrl = baseUrl.TrimEnd('/');
        }

        return new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = $"{baseUrl}/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{baseUrl}/cancel",
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = transaction.Currency,
                        UnitAmountDecimal = (long)(transaction.Amount * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.Description ?? "Artwork purchase"
                        }
                    },
                    Quantity = 1
                }
            ]
        };
    }
}
