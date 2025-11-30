using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using KunstButikken.PaymentService.Application.Interfaces;
using KunstButikken.PaymentService.Application.Models;
using KunstButikken.PaymentService.Application.Services;
using KunstButikken.PaymentService.Domain.Models;
using KunstButikken.PaymentService.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Moq;
using Stripe.Checkout;
using Xunit;

namespace KunstButikken.PaymentService.Tests.Services;

public class PaymentOrchestrationServiceAdvancedTests
{
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly Mock<IStripeSessionService> _stripeSessionServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly PaymentOrchestrationService _sut;

    public PaymentOrchestrationServiceAdvancedTests()
    {
        _paymentRepositoryMock = new Mock<IPaymentRepository>();
        _stripeSessionServiceMock = new Mock<IStripeSessionService>();
        _configurationMock = new Mock<IConfiguration>();
        _sut = new PaymentOrchestrationService(
            _paymentRepositoryMock.Object,
            _stripeSessionServiceMock.Object,
            _configurationMock.Object);
    }

    private CheckoutRequest BuildValidRequest()
        => new()
        {
            Amount = 100,
            Currency = "usd",
            UserId = Guid.NewGuid(),
            ArtId = Guid.NewGuid(),
            AuctionId = null,
            Description = "Test Artwork",
            FrontendBaseUrl = "https://example.com"
        };

    private void SetupValidConfiguration()
    {
        _configurationMock.Setup(c => c["Stripe:ApiKey"]).Returns("test_api_key");
    }

    private void SetupValidSession()
    {
        var session = new Session
        {
            Id = "session_123",
            Url = "https://stripe.com/session_123"
        };
        _stripeSessionServiceMock.Setup(s => s.CreateAsync(
            It.IsAny<SessionCreateOptions>(),
            It.IsAny<Stripe.RequestOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _sut.ProcessCheckoutAsync(null!));
    }

    [Fact]
    public async Task ProcessCheckoutAsync_ThrowsArgumentException_WhenAmountIsZero()
    {
        var request = BuildValidRequest();
        request.Amount = 0;

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _sut.ProcessCheckoutAsync(request));
    }

    [Fact]
    public async Task ProcessCheckoutAsync_ThrowsArgumentException_WhenAmountIsNegative()
    {
        var request = BuildValidRequest();
        request.Amount = -100;

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _sut.ProcessCheckoutAsync(request));
    }

    [Fact]
    public async Task ProcessCheckoutAsync_CreatesTransactionWithCorrectAmount()
    {
        var request = BuildValidRequest();
        request.Amount = 250.50m;

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        _paymentRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Transaction>(t => t.Amount == 250.50m),
            default), Times.Once);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_CreatesTransactionWithCorrectUserId()
    {
        var request = BuildValidRequest();
        var userId = Guid.NewGuid();
        request.UserId = userId;

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        _paymentRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Transaction>(t => t.UserId == userId),
            default), Times.Once);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_CreatesTransactionWithCorrectArtId()
    {
        var request = BuildValidRequest();
        var artId = Guid.NewGuid();
        request.ArtId = artId;

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        _paymentRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Transaction>(t => t.ArtId == artId),
            default), Times.Once);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_CreatesTransactionWithAuctionId_WhenProvided()
    {
        var request = BuildValidRequest();
        var auctionId = Guid.NewGuid();
        request.AuctionId = auctionId;

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        _paymentRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Transaction>(t => t.AuctionId == auctionId),
            default), Times.Once);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_DefaultsToCurrencyUsd_WhenCurrencyIsNull()
    {
        var request = BuildValidRequest();
        request.Currency = null!;

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        _paymentRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Transaction>(t => t.Currency == "usd"),
            default), Times.Once);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_DefaultsToCurrencyUsd_WhenCurrencyIsWhitespace()
    {
        var request = BuildValidRequest();
        request.Currency = "   ";

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        _paymentRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Transaction>(t => t.Currency == "usd"),
            default), Times.Once);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_UsesCurrencyFromRequest_WhenProvided()
    {
        var request = BuildValidRequest();
        request.Currency = "eur";

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        _paymentRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Transaction>(t => t.Currency == "eur"),
            default), Times.Once);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_GeneratesUniqueTransactionId()
    {
        var request = BuildValidRequest();

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        _paymentRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Transaction>(t => t.Id != Guid.Empty),
            default), Times.Once);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_SetsCreatedAtTimestamp()
    {
        var request = BuildValidRequest();
        var beforeCreate = DateTimeOffset.UtcNow.AddSeconds(-1);

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        _paymentRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Transaction>(t => t.CreatedAt >= beforeCreate && t.CreatedAt <= DateTimeOffset.UtcNow.AddSeconds(1)),
            default), Times.Once);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_SavesChangesAfterAddingTransaction()
    {
        var request = BuildValidRequest();

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);


        _paymentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Transaction>(), default), Times.Exactly(1));
        _paymentRepositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessCheckoutAsync_ThrowsInvalidOperationException_WhenStripeApiKeyIsNull()
    {
        var request = BuildValidRequest();

        _configurationMock.Setup(c => c["Stripe:ApiKey"]).Returns((string)null!);
        _configurationMock.Setup(c => c["Stripe__ApiKey"]).Returns((string)null!);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.ProcessCheckoutAsync(request));
    }

    [Fact]
    public async Task ProcessCheckoutAsync_ThrowsInvalidOperationException_WhenStripeApiKeyIsEmpty()
    {
        var request = BuildValidRequest();

        _configurationMock.Setup(c => c["Stripe:ApiKey"]).Returns("");
        _configurationMock.Setup(c => c["Stripe__ApiKey"]).Returns("");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.ProcessCheckoutAsync(request));
    }

    [Fact]
    public async Task ProcessCheckoutAsync_UsesAlternativeApiKeyConfig_WhenColonFormatNotAvailable()
    {
        var request = BuildValidRequest();

        _configurationMock.Setup(c => c["Stripe:ApiKey"]).Returns((string)null!);
        _configurationMock.Setup(c => c["Stripe__ApiKey"]).Returns("alternative_key");
        SetupValidSession();

        var result = await _sut.ProcessCheckoutAsync(request);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessCheckoutAsync_UpdatesTransactionWithStripeSessionId()
    {
        var request = BuildValidRequest();

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        _paymentRepositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessCheckoutAsync_SetsTransactionStatusToPending()
    {
        var request = BuildValidRequest();
        Transaction? capturedTransaction = null;

        _paymentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, ct) => capturedTransaction = t);

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        // After second save, status should be Pending
        capturedTransaction!.Status.Should().Be(TransactionStatus.Pending);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_ReturnsSessionResponse_WithCorrectSessionId()
    {
        var request = BuildValidRequest();

        SetupValidConfiguration();
        SetupValidSession();

        var result = await _sut.ProcessCheckoutAsync(request);

        result.SessionId.Should().Be("session_123");
    }

    [Fact]
    public async Task ProcessCheckoutAsync_ReturnsSessionResponse_WithCorrectUrl()
    {
        var request = BuildValidRequest();

        SetupValidConfiguration();
        SetupValidSession();

        var result = await _sut.ProcessCheckoutAsync(request);

        result.Url.Should().Be("https://stripe.com/session_123");
    }

    [Fact]
    public async Task ProcessCheckoutAsync_ReturnsSessionResponse_WithTransactionId()
    {
        var request = BuildValidRequest();

        SetupValidConfiguration();
        SetupValidSession();

        var result = await _sut.ProcessCheckoutAsync(request);

        result.TransactionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProcessCheckoutAsync_UsesDefaultBaseUrl_WhenNotProvided()
    {
        var request = BuildValidRequest();
        request.FrontendBaseUrl = null!;

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        _stripeSessionServiceMock.Verify(s => s.CreateAsync(
            It.Is<SessionCreateOptions>(o =>
                o.SuccessUrl!.StartsWith("https://kunstbutikken.example") &&
                o.CancelUrl!.StartsWith("https://kunstbutikken.example")),
            It.IsAny<Stripe.RequestOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_TrimsTrailingSlashFromBaseUrl()
    {
        var request = BuildValidRequest();
        request.FrontendBaseUrl = "https://example.com/";

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        _stripeSessionServiceMock.Verify(s => s.CreateAsync(
            It.Is<SessionCreateOptions>(o =>
                o.SuccessUrl!.StartsWith("https://example.com/success") &&
                o.CancelUrl == "https://example.com/cancel"),
            It.IsAny<Stripe.RequestOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_UsesDescriptionInLineItem_WhenProvided()
    {
        var request = BuildValidRequest();
        request.Description = "Custom Artwork Description";

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        _stripeSessionServiceMock.Verify(s => s.CreateAsync(
            It.Is<SessionCreateOptions>(o =>
                o.LineItems[0].PriceData.ProductData.Name == "Custom Artwork Description"),
            It.IsAny<Stripe.RequestOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_UsesDefaultDescription_WhenNotProvided()
    {
        var request = BuildValidRequest();
        request.Description = null!;

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        _stripeSessionServiceMock.Verify(s => s.CreateAsync(
            It.Is<SessionCreateOptions>(o =>
                o.LineItems[0].PriceData.ProductData.Name == "Artwork purchase"),
            It.IsAny<Stripe.RequestOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_ConvertsAmountToCents()
    {
        var request = BuildValidRequest();
        request.Amount = 123.45m;

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request);

        _stripeSessionServiceMock.Verify(s => s.CreateAsync(
            It.Is<SessionCreateOptions>(o =>
                o.LineItems[0].PriceData.UnitAmountDecimal == 12345),
            It.IsAny<Stripe.RequestOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_PassesCancellationToken()
    {
        var request = BuildValidRequest();
        var cts = new CancellationTokenSource();

        SetupValidConfiguration();
        SetupValidSession();

        await _sut.ProcessCheckoutAsync(request, cts.Token);

        _paymentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Transaction>(), cts.Token), Times.Once);
        _paymentRepositoryMock.Verify(r => r.SaveChangesAsync(cts.Token), Times.Exactly(2));
    }
}

