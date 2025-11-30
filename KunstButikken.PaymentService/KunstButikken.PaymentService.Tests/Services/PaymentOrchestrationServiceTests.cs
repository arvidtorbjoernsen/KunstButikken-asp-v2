using Moq;
using FluentAssertions;
using KunstButikken.PaymentService.Application.Services;
using KunstButikken.PaymentService.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Xunit;
using KunstButikken.PaymentService.Application.Interfaces;
using KunstButikken.PaymentService.Application.Models;
using Stripe.Checkout;
using KunstButikken.PaymentService.Domain.Models;

namespace KunstButikken.PaymentService.Tests.Services;

public class PaymentOrchestrationServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly Mock<IStripeSessionService> _stripeSessionServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly PaymentOrchestrationService _paymentService;

    public PaymentOrchestrationServiceTests()
    {
        _paymentRepositoryMock = new Mock<IPaymentRepository>();
        _stripeSessionServiceMock = new Mock<IStripeSessionService>();
        _configurationMock = new Mock<IConfiguration>();
        _paymentService = new PaymentOrchestrationService(
            _paymentRepositoryMock.Object,
            _stripeSessionServiceMock.Object,
            _configurationMock.Object);
    }

    private static CheckoutRequest BuildCheckoutRequest()
        => new()
        {
            Amount = 100,
            Currency = "usd",
            UserId = Guid.NewGuid(),
            ArtId = Guid.NewGuid(),
            AuctionId = Guid.NewGuid(),
            Description = "Test Artwork",
            FrontendBaseUrl = "https://example.com"
        };

    [Fact]
    public async Task ProcessCheckoutAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
    {
        CheckoutRequest? request = null;

        Func<Task> act = async () => await _paymentService.ProcessCheckoutAsync(request!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProcessCheckoutAsync_WhenAmountIsZero_ShouldThrowArgumentException()
    {
        var request = BuildCheckoutRequest();
        request.Amount = 0;

        Func<Task> act = async () => await _paymentService.ProcessCheckoutAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ProcessCheckoutAsync_WhenStripeApiKeyIsNotConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = BuildCheckoutRequest();
        _configurationMock.Setup(c => c["Stripe:ApiKey"]).Returns((string)null);
        _configurationMock.Setup(c => c["Stripe__ApiKey"]).Returns((string)null);

        // Act
        Func<Task> act = async () => await _paymentService.ProcessCheckoutAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ProcessCheckoutAsync_WhenRequestIsValid_ShouldReturnSessionResponse()
    {
        // Arrange
        var request = BuildCheckoutRequest();

        _configurationMock.Setup(c => c["Stripe:ApiKey"]).Returns("test_api_key");

        var session = new Session
        {
            Id = "session_123",
            Url = "https://stripe.com/session_123"
        };

        _stripeSessionServiceMock.Setup(s => s.CreateAsync(It.IsAny<SessionCreateOptions>(), default))
            .ReturnsAsync(session);

        // Act
        var result = await _paymentService.ProcessCheckoutAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(session.Id);
        result.Url.Should().Be(session.Url);
        result.TransactionId.Should().NotBeEmpty();

        _paymentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Transaction>(), default), Times.Once);
        _paymentRepositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessCheckoutAsync_WhenCurrencyIsNull_ShouldDefaultToUsd()
    {
        // Arrange
        var request = BuildCheckoutRequest();
        request.Currency = null!;

        _configurationMock.Setup(c => c["Stripe:ApiKey"]).Returns("test_api_key");

        var session = new Session
        {
            Id = "session_123",
            Url = "https://stripe.com/session_123"
        };

        _stripeSessionServiceMock.Setup(s => s.CreateAsync(It.IsAny<SessionCreateOptions>(), default))
            .ReturnsAsync(session);

        // Act
        await _paymentService.ProcessCheckoutAsync(request);

        // Assert
        _paymentRepositoryMock.Verify(r => r.AddAsync(It.Is<Transaction>(t => t.Currency == "usd"), default), Times.Once);
    }
}
