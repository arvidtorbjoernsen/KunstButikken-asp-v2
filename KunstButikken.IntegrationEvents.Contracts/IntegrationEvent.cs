namespace KunstButikken.IntegrationEvents.Contracts;

public record IntegrationEvent(Guid Id, DateTimeOffset CreatedAt)
{
  public IntegrationEvent() : this(Guid.NewGuid(), DateTimeOffset.UtcNow)
  {
  }
}