namespace KunstButikken.UserService.Logging;

// Small helper to create and cache LoggerMessage delegates with less boilerplate.
public static class Log
{
    // Create a delegate for messages with no params (except optional Exception)
    public static Action<ILogger, Exception?> Define(LogLevel level, EventId eventId, string message) =>
        LoggerMessage.Define(level, eventId, message);

    // With one template parameter
    public static Action<ILogger, T1, Exception?> Define<T1>(LogLevel level, EventId eventId, string message) =>
        LoggerMessage.Define<T1>(level, eventId, message);

    // With two parameters
    public static Action<ILogger, T1, T2, Exception?> Define<T1, T2>(LogLevel level, EventId eventId, string message) =>
        LoggerMessage.Define<T1, T2>(level, eventId, message);

    // With three parameters
    public static Action<ILogger, T1, T2, T3, Exception?> Define<T1, T2, T3>(LogLevel level, EventId eventId, string message) =>
        LoggerMessage.Define<T1, T2, T3>(level, eventId, message);

    // Additional overloads can be added as needed
}
