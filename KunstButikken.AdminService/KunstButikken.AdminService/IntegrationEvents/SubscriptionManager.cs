using System.Collections.Concurrent;
using KunstButikken.IntegrationEvents.Contracts;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;

// Updated namespace

namespace KunstButikken.AdminService.IntegrationEvents;

public class SubscriptionManager : ISubscriptionManager
{
    private readonly ConcurrentDictionary<string, Type> _eventTypes = new();
    private readonly ConcurrentDictionary<string, List<Type>> _handlers = new();

    public void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventConsumer<T>
    {
        var name = typeof(T).Name;
        _handlers.AddOrUpdate(name, _ => [typeof(TH)], (_, list) =>
        {
            if (!list.Contains(typeof(TH)))
            {
                list.Add(typeof(TH));
            }

            return list;
        });
        _eventTypes.TryAdd(name, typeof(T));
    }

    public void AddSubscription(Type eventType, Type handlerType)
    {
        if (eventType == null)
        {
            throw new ArgumentNullException(nameof(eventType));
        }

        if (handlerType == null)
        {
            throw new ArgumentNullException(nameof(handlerType));
        }

        var eventName = eventType.Name;
        _handlers.AddOrUpdate(eventName, _ => [handlerType], (_, list) =>
        {
            if (!list.Contains(handlerType))
            {
                list.Add(handlerType);
            }

            return list;
        });
        _eventTypes.TryAdd(eventName, eventType);
    }

    public bool HasSubscriptionsForEvent(string eventName)
    {
        if (eventName == null)
        {
            throw new ArgumentNullException(nameof(eventName));
        }

        return _handlers.ContainsKey(eventName);
    }

    public Type GetEventTypeByName(string eventName)
    {
        if (eventName == null)
        {
            throw new ArgumentNullException(nameof(eventName));
        }

        return _eventTypes[eventName];
    }

    public IEnumerable<Type> GetHandlersForEvent(string eventName)
    {
        if (eventName == null)
        {
            throw new ArgumentNullException(nameof(eventName));
        }

        return _handlers.TryGetValue(eventName, out var list) ? list : Enumerable.Empty<Type>();
    }
}
