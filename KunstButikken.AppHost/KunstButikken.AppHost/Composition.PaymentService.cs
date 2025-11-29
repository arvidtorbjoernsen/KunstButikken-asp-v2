namespace KunstButikken.AppHost;

public static partial class AppCompositionBuilder
{
    private static (IResourceBuilder<IResourceWithEnvironment>, IResourceBuilder<ProjectResource>) BuildPaymentService(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> paymentsDb,
        IResourceBuilder<RabbitMQServerResource> eventBus,
        IResourceBuilder<PostgresServerResource> postgres,
        string? stripeApiKey,
        string? stripeWebhookSecret,
        KeycloakSettings keycloakSettings)
    {
        var pay = builder
            .AddProject("payment-service", "../../KunstButikken.PaymentService/KunstButikken.PaymentService/KunstButikken.PaymentService.csproj")
            .WithReference(paymentsDb)
            .WithReference(eventBus)
            .WithEnvironment("ConnectionStrings__Default", paymentsDb)
            .WithEnvironment("Stripe__ApiKey", stripeApiKey)
            .WithEnvironment("Stripe__WebhookSecret", stripeWebhookSecret)
            .WithHttpEndpoint(name: "api")
            .WithHttpEndpoint(name: "webhook")
            .WithExternalHttpEndpoints()
            .WaitFor(postgres ?? throw new InvalidOperationException("postgres is null"))
            .WaitFor(eventBus ?? throw new InvalidOperationException("eventBus is null"));

        pay = ApplyKeycloakEnvironment(pay, keycloakSettings);
        pay.WaitForHttp("/api/health");
        return (pay, pay);
    }
}
