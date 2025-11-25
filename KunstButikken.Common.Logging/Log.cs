using Microsoft.Extensions.Logging;

namespace KunstButikken.Common.Logging;

public static class Log
{
    public static Action<ILogger, Exception?> Define(LogLevel level, EventId id, string message)
        => LoggerMessage.Define(level, id, message);

    public static Action<ILogger, T1, Exception?> Define<T1>(LogLevel level, EventId id, string message)
        => LoggerMessage.Define<T1>(level, id, message);

    public static Action<ILogger, T1, T2, Exception?> Define<T1, T2>(LogLevel level, EventId id, string message)
        => LoggerMessage.Define<T1, T2>(level, id, message);

    public static Action<ILogger, T1, T2, T3, Exception?> Define<T1, T2, T3>(LogLevel level, EventId id, string message)
        => LoggerMessage.Define<T1, T2, T3>(level, id, message);
}
