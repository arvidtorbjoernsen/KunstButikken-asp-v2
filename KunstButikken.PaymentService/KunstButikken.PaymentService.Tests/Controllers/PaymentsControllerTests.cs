using FluentAssertions;
using KunstButikken.PaymentService.Application.Interfaces;
using KunstButikken.PaymentService.Application.Models;
using KunstButikken.PaymentService.Controllers;
using KunstButikken.PaymentService.Domain.Models;
using KunstButikken.PaymentService.Domain.Repositories;
using KunstButikken.PaymentService.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;
using System.Text;

namespace KunstButikken.PaymentService.Tests.Controllers;

public sealed class PaymentsControllerTests
{
    [Fact]
    public async Task CreateCheckoutSession_ReturnsOkWithSessionInfo()
    {
        var paymentService = new Mock<IPaymentService>();
        var repo = new Mock<IPaymentRepository>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var webhook = new Mock<IStripeWebhookService>();

        paymentService
            .Setup(p => p.ProcessCheckoutAsync(It.IsAny<CheckoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionResponse { SessionId = "cs_test", Url = "https://stripe" });

        var controller = new PaymentsController(paymentService.Object, repo.Object, configuration, webhook.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        }));

        var result = await controller.CreateCheckoutSession(new CheckoutRequest { Amount = 10, Currency = "usd" });

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result.Result!;
        ok.Value.Should().BeEquivalentTo(new { sessionId = "cs_test", url = "https://stripe" });
    }

    [Fact]
    public async Task CreateCheckoutSession_InvalidAmount_ReturnsBadRequest()
    {
        var controller = new PaymentsController(Mock.Of<IPaymentService>(), Mock.Of<IPaymentRepository>(),
            new ConfigurationBuilder().Build(), Mock.Of<IStripeWebhookService>());

        var result = await controller.CreateCheckoutSession(new CheckoutRequest { Amount = 0 });

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Webhook_CompletesSession_UpdatesTransaction()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new PaymentDbContext(options);
        var transaction = new Transaction { Id = Guid.NewGuid(), StripeSessionId = "cs_test", Status = TransactionStatus.Pending };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        var repo = new PaymentRepository(db);

        var stripeEvent = new Event
        {
            Type = "checkout.session.completed",
            Data = new EventData
            {
                Object = new Session { Id = "cs_test", PaymentIntentId = "pi_123" }
            }
        };

        var webhook = new Mock<IStripeWebhookService>();
        webhook.Setup(w => w.ConstructEvent(It.IsAny<string>(), It.IsAny<string>())).Returns(stripeEvent);

        var controller = new PaymentsController(Mock.Of<IPaymentService>(), repo, new ConfigurationBuilder().Build(), webhook.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.ControllerContext.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        controller.ControllerContext.HttpContext.Request.Headers["Stripe-Signature"] = "sig";

        var result = await controller.Webhook();

        result.Should().BeOfType<OkResult>();
        var updated = await db.Transactions.FirstAsync(t => t.Id == transaction.Id);
        updated.Status.Should().Be(TransactionStatus.Succeeded);
        updated.StripePaymentIntentId.Should().Be("pi_123");
    }

    [Fact]
    public async Task Webhook_InvalidSignature_ReturnsBadRequest()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new PaymentDbContext(options);
        var repo = new PaymentRepository(db);

        var webhook = new Mock<IStripeWebhookService>();
        webhook.Setup(w => w.ConstructEvent(It.IsAny<string>(), It.IsAny<string>())).Throws(new StripeException("bad"));

        var controller = new PaymentsController(Mock.Of<IPaymentService>(), repo,
            new ConfigurationBuilder().Build(), webhook.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.ControllerContext.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

        var result = await controller.Webhook();

        result.Should().BeOfType<BadRequestResult>();
    }
}
