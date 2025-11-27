using KunstButikken.PaymentService.Application.Interfaces;
using KunstButikken.PaymentService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KunstButikken.PaymentService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPaymentService, PaymentOrchestrationService>();
        services.AddScoped<IStripeSessionService, StripeSessionService>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();
        return services;
    }
}
