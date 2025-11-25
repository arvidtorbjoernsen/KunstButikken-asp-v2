using KunstButikken.IntegrationEvents.Contracts;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;

// Updated namespace

namespace KunstButikken.PaymentService.IntegrationEvents;

public class SubscriptionManager : ISubscriptionManager
{
    private readonly Dictionary<string, Type> _eventTypes = new();
    private readonly Dictionary<string, List<Type>> _handlers = new();

    public void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventConsumer<T>
    {
        var eventName = typeof(T).Name;
        if (!_handlers.TryGetValue(eventName, out var list))
        {
            list = new List<Type>();
            _handlers[eventName] = list;
        }

        list.Add(typeof(TH));
        _eventTypes[eventName] = typeof(T);
    }

    public void AddSubscription(Type eventType, Type handlerType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(handlerType);

        var eventName = eventType.Name;
        if (!_handlers.TryGetValue(eventName, out var list))
        {
            list = new List<Type>();
            _handlers[eventName] = list;
        }

        list.Add(handlerType);
        _eventTypes[eventName] = eventType;
    }

    public bool HasSubscriptionsForEvent(string eventName)
    {
        ArgumentNullException.ThrowIfNull(eventName);
        return _handlers.ContainsKey(eventName);
    }

    public IEnumerable<Type> GetHandlersForEvent(string eventName)
    {
        ArgumentNullException.ThrowIfNull(eventName);
        return _handlers.TryGetValue(eventName, out var list) ? list : Enumerable.Empty<Type>();
    }

    public Type GetEventTypeByName(string eventName)
    {
        ArgumentNullException.ThrowIfNull(eventName);
        return _eventTypes[eventName];
    }
}
