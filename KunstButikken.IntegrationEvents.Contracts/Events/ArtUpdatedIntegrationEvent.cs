namespace KunstButikken.IntegrationEvents.Contracts.Events;

public record ArtUpdatedIntegrationEvent(
  Guid Id,
  string TitleEn,
  string TitleNb,
  string Artist,
  Guid SellerId,
  string SellerDisplayName,
  decimal Price,
  Uri ImageUrl,
  string Status,
  bool IsVerified)
  : IntegrationEvent(Id, DateTimeOffset.UtcNow);