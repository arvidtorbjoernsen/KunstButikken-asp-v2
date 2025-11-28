using FluentAssertions;
using KunstButikken.PaymentService.Application.Interfaces;
using KunstButikken.PaymentService.Application.Models;
using KunstButikken.PaymentService.Application.Services;
using KunstButikken.PaymentService.Domain.Models;
using KunstButikken.PaymentService.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Moq;
using Stripe.Checkout;

namespace KunstButikken.PaymentService.Tests.Services;

public sealed class PaymentOrchestrationServiceTests
{
    [Fact]
    public async Task ProcessCheckoutAsync_PersistsTransactionAndReturnsSession()
    {
        // Arrange
        var repo = new Mock<IPaymentRepository>();
        var sessionService = new Mock<IStripeSessionService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:ApiKey"] = "sk_test_123"
            }!)
            .Build();

        var createdTransaction = new Transaction();
        repo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Returns<Transaction, CancellationToken>((tx, _) =>
            {
                createdTransaction = tx;
                return Task.CompletedTask;
            });
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        sessionService.Setup(s => s.CreateAsync(It.IsAny<SessionCreateOptions>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Session
            {
                Id = "cs_test_123",
                Url = "https://checkout.stripe.com/pay/cs_test_123"
            });

        var sut = new PaymentOrchestrationService(repo.Object, sessionService.Object, configuration);

        var request = new CheckoutRequest
        {
            Amount = 100,
            Currency = "usd",
            UserId = Guid.NewGuid(),
            FrontendBaseUrl = "https://frontend"
        };

        // Act
        var response = await sut.ProcessCheckoutAsync(request);

        // Assert
        response.SessionId.Should().Be("cs_test_123");
        response.Url.Should().Contain("stripe.com");
        repo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
        createdTransaction.Amount.Should().Be(100);
        createdTransaction.StripeSessionId.Should().Be("cs_test_123");
    }
}
