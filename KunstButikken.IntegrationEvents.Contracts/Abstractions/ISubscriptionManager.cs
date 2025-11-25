namespace KunstButikken.IntegrationEvents.Contracts.Abstractions;

public interface ISubscriptionManager
{
  void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventConsumer<T>;

  // Non-generic overload to avoid generic constraint issues when using shared contract types
  void AddSubscription(Type eventType, Type handlerType);

  bool HasSubscriptionsForEvent(string eventName);
  IEnumerable<Type> GetHandlersForEvent(string eventName);
  Type GetEventTypeByName(string eventName);
}