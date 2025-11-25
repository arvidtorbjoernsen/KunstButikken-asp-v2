namespace KunstButikken.IntegrationEvents.Contracts.Events;

public record ArtCreatedIntegrationEvent(
  string TitleEn,
  string TitleNb,
  string Artist,
  Guid SellerId,
  string SellerDisplayName,
  decimal Price,
  Uri ImageUrl) : IntegrationEvent;