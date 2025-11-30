namespace KunstButikken.PaymentService.Domain.Models;

public enum TransactionStatus
{
  Pending,
  Succeeded,
  Failed
}

public class Transaction
{
  public Guid Id { get; set; }
  public Guid UserId { get; set; }
  public Guid? AuctionId { get; set; }
  public Guid? ArtId { get; set; }
  public decimal Amount { get; set; }
  public string Currency { get; set; } = "usd";
  public string? StripeSessionId { get; set; }
  public string? StripePaymentIntentId { get; set; }
  public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
  public DateTimeOffset CreatedAt { get; set; }
}
