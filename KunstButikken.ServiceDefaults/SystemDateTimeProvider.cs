namespace KunstButikken.ServiceDefaults;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
  public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
  public DateTimeOffset Now => DateTimeOffset.Now;
}