using KunstButikken.PaymentService.Application.Interfaces;
using KunstButikken.PaymentService.Application.Services;
using KunstButikken.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KunstButikken.PaymentService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPaymentService, PaymentOrchestrationService>();
        services.AddScoped<IStripeSessionService, StripeSessionService>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<RabbitMqPublisher>();
        return services;
    }
}
