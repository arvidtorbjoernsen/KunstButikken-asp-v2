using FluentAssertions;
using KunstButikken.PaymentService.Application.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace KunstButikken.PaymentService.Tests.Services;

public class StripeWebhookServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly StripeWebhookService _webhookService;

    public StripeWebhookServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _webhookService = new StripeWebhookService(_configurationMock.Object);
    }

    [Fact]
    public void ConstructEvent_WhenWebhookSecretIsMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _configurationMock.Setup(c => c["Stripe:WebhookSecret"]).Returns((string)null);
        _configurationMock.Setup(c => c["Stripe__WebhookSecret"]).Returns((string)null);

        // Act
        Action act = () => _webhookService.ConstructEvent("payload", "signature");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Stripe webhook secret missing");
    }
}
