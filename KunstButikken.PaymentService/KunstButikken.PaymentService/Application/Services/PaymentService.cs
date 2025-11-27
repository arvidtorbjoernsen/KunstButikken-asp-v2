using KunstButikken.PaymentService.Application.Interfaces;

namespace KunstButikken.PaymentService.Application.Services;

public sealed class PaymentOrchestrationService : IPaymentService
{
    private readonly IStripeSessionService _sessionService;

    public PaymentOrchestrationService(IStripeSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public async Task ProcessCheckoutAsync(object request, CancellationToken cancellationToken = default)
    {
        // Placeholder orchestration logic; connect to Application DTOs later
        await Task.CompletedTask;
    }
}
