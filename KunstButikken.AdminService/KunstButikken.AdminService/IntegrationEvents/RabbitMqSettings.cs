namespace KunstButikken.AdminService.IntegrationEvents;

public class RabbitMqSettings
{
  public string ExchangeName { get; set; } = "kunstbutikken_exchange";
  public string QueueName { get; set; } = "admin-service-queue";
  public string DeadLetterExchangeName { get; set; } = "kunstbutikken_deadletter";
  public int RetryCount { get; set; } = 3;
}