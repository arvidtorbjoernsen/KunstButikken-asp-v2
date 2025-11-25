namespace KunstButikken.IntegrationEvents.Contracts.Events;

public record AdminUserCreatedIntegrationEvent(string Email, string DisplayName) : IntegrationEvent;