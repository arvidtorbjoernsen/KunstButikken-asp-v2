namespace KunstButikken.IntegrationEvents.Contracts.Events;

public record AuctionEndedIntegrationEvent(Guid AuctionId, Guid WinnerId, decimal FinalPrice) : IntegrationEvent;