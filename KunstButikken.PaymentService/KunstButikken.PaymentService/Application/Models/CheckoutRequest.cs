namespace KunstButikken.PaymentService.Application.Models;

public class CheckoutRequest
{
    public Guid? AuctionId { get; set; }
    public Guid? ArtId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public Guid UserId { get; set; }
    public string FrontendBaseUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class SessionResponse
{
    public required string SessionId { get; init; }
    public string? Url { get; init; }
    public Guid TransactionId { get; init; }
}
