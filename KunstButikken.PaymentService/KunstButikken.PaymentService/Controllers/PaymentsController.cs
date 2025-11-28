using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KunstButikken.PaymentService.Application.Interfaces;
using KunstButikken.PaymentService.Application.Models;
using KunstButikken.PaymentService.Domain.Models;
using KunstButikken.PaymentService.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace KunstButikken.PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(
    IPaymentService paymentService,
    IPaymentRepository repo,
    IConfiguration cfg,
    IStripeWebhookService stripeWebhookService) : ControllerBase
{
    [HttpPost("checkout-session")]
    public async Task<ActionResult<object>> CreateCheckoutSession([FromBody] CheckoutRequest input)
    {
        if (input == null)
        {
            return BadRequest(new { error = "Missing request body" });
        }

        if (input.Amount <= 0)
        {
            return BadRequest(new { error = "Amount must be greater than 0" });
        }

        input.Currency = string.IsNullOrWhiteSpace(input.Currency) ? "usd" : input.Currency;
        input.UserId = GetUserId();
        input.FrontendBaseUrl = ResolveFrontendBaseUrl();

        try
        {
            var response = await paymentService.ProcessCheckoutAsync(input).ConfigureAwait(false);
            return Ok(new { sessionId = response.SessionId, url = response.Url });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(501, new { error = ex.Message });
        }
        catch (StripeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var sr = new StreamReader(HttpContext.Request.Body, Encoding.UTF8, true, 1024, true);
        var json = await sr.ReadToEndAsync().ConfigureAwait(false);
        try
        {
            var stripeEvent = stripeWebhookService.ConstructEvent(json,
                (string?)Request.Headers["Stripe-Signature"] ?? string.Empty);
            if (stripeEvent.Type == "checkout.session.completed")
            {
                if (stripeEvent.Data.Object is Session session)
                {
                    var tx = await repo.Query()
                        .FirstOrDefaultAsync(t => t.StripeSessionId == session.Id)
                        .ConfigureAwait(false);
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

    private string ResolveFrontendBaseUrl()
    {
        var frontendBase = cfg["Frontend:BaseUrl"] ?? cfg["Frontend__BaseUrl"];
        if (string.IsNullOrWhiteSpace(frontendBase))
        {
            frontendBase = Request.Scheme + "://" + Request.Host;
        }

        return frontendBase.EndsWith('/') ? frontendBase.TrimEnd('/') : frontendBase;
    }
}
