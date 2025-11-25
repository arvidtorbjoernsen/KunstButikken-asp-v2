namespace KunstButikken.ArtService.IntegrationEvents;

public class RabbitMqSettings
{
  public string ExchangeName { get; set; } = "kunstbutikken_event_bus";
  public string QueueName { get; set; } = "kunstbutikken_art_service";
  public string DeadLetterExchangeName { get; set; } = "kunstbutikken_dead_letter_exchange";
  public int RetryCount { get; set; } = 5;
}