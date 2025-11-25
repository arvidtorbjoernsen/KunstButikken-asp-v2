namespace KunstButikken.ServiceDefaults;

public interface IDateTimeProvider
{
  DateTimeOffset UtcNow { get; }
  DateTimeOffset Now { get; }
}