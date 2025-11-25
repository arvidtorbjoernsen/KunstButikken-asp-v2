using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KunstButikken.PaymentService.Data;
using KunstButikken.PaymentService.Models;
using KunstButikken.PaymentService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace KunstButikken.PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(
    IPaymentRepository repo,
    IConfiguration cfg,
    IStripeWebhookService stripeWebhookService,
    IStripeSessionService stripeSessionService) : ControllerBase
{
    [HttpPost("checkout-session")]
    public Task<ActionResult<object>> CreateCheckoutSession([FromBody] Transaction input) =>
        CreateCheckoutSessionImpl(input);

    private async Task<ActionResult<object>> CreateCheckoutSessionImpl(Transaction input)
    {
        var apiKey = ValidateStripeConfigOrFail();
        if (apiKey == null)
        {
            return StatusCode(501,
                new { error = "Stripe API key is not configured. Set Stripe:ApiKey or Stripe__ApiKey." });
        }

        StripeConfiguration.ApiKey = apiKey;

        var validation = ValidateTransactionInput(input);
        if (validation != null)
        {
            return validation;
        }

        var tx = await CreateAndPersistTransactionAsync(input).ConfigureAwait(false);

        var frontendBase = ResolveFrontendBaseUrl();

        var options = BuildSessionOptions(tx, frontendBase);
        try
        {
            var session = await stripeSessionService.CreateAsync(options).ConfigureAwait(false);
            tx.StripeSessionId = session.Id;
            await repo.SaveChangesAsync().ConfigureAwait(false);
            return Ok(new { sessionId = session.Id, url = session.Url });
        }
        catch (StripeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private string? ValidateStripeConfigOrFail()
    {
        return string.IsNullOrWhiteSpace(cfg["Stripe:ApiKey"] ?? cfg["Stripe__ApiKey"])
            ? null
            : cfg["Stripe:ApiKey"] ?? cfg["Stripe__ApiKey"];
    }

    private ActionResult<object>? ValidateTransactionInput(Transaction input)
    {
        if (input == null)
        {
            return BadRequest(new { error = "Missing request body" });
        }

        if (input.Amount <= 0)
        {
            return BadRequest(new { error = "Amount must be greater than 0" });
        }

        if (string.IsNullOrWhiteSpace(input.Currency))
        {
            input.Currency = "usd";
        }

        return null;
    }

    private async Task<Transaction> CreateAndPersistTransactionAsync(Transaction input)
    {
        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ArtId = input.ArtId,
            AuctionId = input.AuctionId,
            Amount = input.Amount,
            Currency = input.Currency
        };
        await repo.AddAsync(tx).ConfigureAwait(false);
        await repo.SaveChangesAsync().ConfigureAwait(false);
        return tx;
    }

    private string ResolveFrontendBaseUrl()
    {
        var frontendBase = cfg["Frontend:BaseUrl"] ?? cfg["Frontend__BaseUrl"];
        if (string.IsNullOrWhiteSpace(frontendBase))
        {
            frontendBase = Request.Scheme + "://" + Request.Host;
        }

        if (frontendBase.EndsWith('/'))
        {
            frontendBase = frontendBase.TrimEnd('/');
        }

        return frontendBase;
    }

    private static SessionCreateOptions BuildSessionOptions(Transaction tx, string frontendBase)
    {
        return new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = frontendBase + "/success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = frontendBase + "/cancel",
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = tx.Currency,
                        UnitAmountDecimal = (long)(tx.Amount * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = "Art purchase" }
                    },
                    Quantity = 1
                }
            ]
        };
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        // Read the request body without closing the underlying HttpContext.Request.Body stream
        using var sr = new StreamReader(HttpContext.Request.Body, Encoding.UTF8, true, 1024, true);
        var json = await sr.ReadToEndAsync().ConfigureAwait(false);
        var secret = cfg["Stripe:WebhookSecret"] ?? cfg["Stripe__WebhookSecret"];
        try
        {
            var stripeEvent = stripeWebhookService.ConstructEvent(json,
                (string?)Request.Headers["Stripe-Signature"] ?? string.Empty,
                secret ?? string.Empty); // Explicitly cast to string?
            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null)
                {
                    var tx = await repo.Query().FirstOrDefaultAsync(t => t.StripeSessionId == session.Id).ConfigureAwait(false);
                    if (tx != null)
                    {
                        tx.Status = TransactionStatus.Succeeded;
                        tx.StripePaymentIntentId = session.PaymentIntentId;
                        await repo.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
            }

            return Ok();
        }
        catch (StripeException)
        {
            return BadRequest();
        }
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(sub))
        {
            throw new InvalidOperationException("Missing subject (sub)");
        }

        return Guid.TryParse(sub, out var guid) ? guid : CreateDeterministicGuid(sub);
    }

    private static Guid CreateDeterministicGuid(string input)
    {
        // Use SHA-256 instead of MD5 (CA5351) and truncate to 16 bytes for deterministic GUID.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var g = new byte[16];
        Array.Copy(hash, g, 16);
        // set variant (RFC 4122)
        g[8] = (byte)((g[8] & 0x3F) | 0x80);
        // set version to 5 (name-based); SHA-256 isn't v5 but this keeps GUID version field meaningful
        g[6] = (byte)((g[6] & 0x0F) | (5 << 4));
        return new Guid(g);
    }
}
