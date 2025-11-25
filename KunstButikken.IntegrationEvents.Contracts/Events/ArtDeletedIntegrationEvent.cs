namespace KunstButikken.IntegrationEvents.Contracts.Events;

public record ArtDeletedIntegrationEvent(Guid ArtId) : IntegrationEvent;