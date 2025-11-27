using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using KunstButikken.IntegrationEvents.Contracts;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

// Updated namespace

namespace KunstButikken.PaymentService.IntegrationEvents;

internal sealed class RabbitMqEventBus : IEventBus, IDisposable
{
    private static readonly Action<ILogger<RabbitMqEventBus>, string, double, Exception?> _logRetry
        = LoggerMessage.Define<string, double>(LogLevel.Warning, new EventId(1000, nameof(_logRetry)),
            "Could not process event: {Message}. Retrying in {Seconds}s");

    private static readonly Action<ILogger<RabbitMqEventBus>, string, Exception?> _logPublished
        = LoggerMessage.Define<string>(LogLevel.Information, new EventId(1001, nameof(_logPublished)),
            "Published event {EventName}");

    private static readonly Action<ILogger<RabbitMqEventBus>, string, Exception?> _logDeserializeWarning
        = LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1002, nameof(_logDeserializeWarning)),
            "Failed to deserialize message for event {EventName}");

    private static readonly Action<ILogger<RabbitMqEventBus>, string, Exception?> _logProcessError
        = LoggerMessage.Define<string>(LogLevel.Error, new EventId(1003, nameof(_logProcessError)),
            "Error processing event {EventName} after retries.");

    private readonly SemaphoreSlim _channelLock = new(1, 1);
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly IConnection _rabbitMqConnection;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqSettings _settings;
    private readonly ISubscriptionManager _subscriptionManager;
    private IChannel? _channel;

    public RabbitMqEventBus(
        IConnection rabbitMqConnection,
        ILogger<RabbitMqEventBus> logger,
        IOptions<RabbitMqSettings> options,
        ISubscriptionManager subscriptionManager,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(rabbitMqConnection);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(subscriptionManager);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _rabbitMqConnection = rabbitMqConnection;
        _logger = logger;
        _settings = options.Value;
        _subscriptionManager = subscriptionManager;
        _serviceProvider = serviceProvider;
    }

    // Implement Dispose pattern
    public void Dispose() => Dispose(true);

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        await _channelLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _channel ??= await _rabbitMqConnection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var eventName = integrationEvent.GetType().Name;
            await _channel.ExchangeDeclareAsync(_settings.ExchangeName, ExchangeType.Fanout, true, false).ConfigureAwait(false);

            var json = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());
            var body = Encoding.UTF8.GetBytes(json);

            await _channel.BasicPublishAsync(_settings.ExchangeName, eventName, body, cancellationToken).ConfigureAwait(false);
            _logPublished(_logger, eventName, null);
        }
        finally
        {
            _channelLock.Release();
        }
    }

    public void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventConsumer<T>
    {
        // Keep compatibility — start subscription asynchronously and don't await here
        _ = SubscribeAsync<T, TH>();
    }

    public async Task SubscribeAsync<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventConsumer<T>
    {
        var eventName = typeof(T).Name;
        // Use generic subscription manager API
        _subscriptionManager.AddSubscription<T, TH>();

        await _channelLock.WaitAsync().ConfigureAwait(false);
        try
        {
            _channel ??= await _rabbitMqConnection.CreateChannelAsync().ConfigureAwait(false);
            await _channel.ExchangeDeclareAsync(_settings.ExchangeName, ExchangeType.Fanout, true, false).ConfigureAwait(false);

            var queueArgs = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", _settings.DeadLetterExchangeName }
            };
            await _channel.QueueDeclareAsync(_settings.QueueName, true, false, false, queueArgs).ConfigureAwait(false);
            await _channel.QueueBindAsync(_settings.QueueName, _settings.ExchangeName, eventName).ConfigureAwait(false);

            await _channel.ExchangeDeclareAsync(_settings.DeadLetterExchangeName, ExchangeType.Fanout, true, false).ConfigureAwait(false);
            var deadLetterQueueName = $"{_settings.QueueName}_deadletter";
            await _channel.QueueDeclareAsync(deadLetterQueueName, true, false, false).ConfigureAwait(false);
            await _channel.QueueBindAsync(deadLetterQueueName, _settings.DeadLetterExchangeName, string.Empty).ConfigureAwait(false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    await ProcessEvent(ea.RoutingKey, message, ea.DeliveryTag).ConfigureAwait(false);
                }
                catch
                {
                    // swallow
                }
            };

            await _channel.BasicConsumeAsync(_settings.QueueName, false, consumer).ConfigureAwait(false);
        }
        finally
        {
            _channelLock.Release();
        }
    }

    private void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        // synchronously wait for async cleanup to keep API compatible
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            try { await _channel.CloseAsync().ConfigureAwait(false); } catch { }
            try { await _channel.DisposeAsync().ConfigureAwait(false); } catch { }
            _channel = null;
        }

        _channelLock.Dispose();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Event processing must catch all exceptions to nack messages and avoid crashing the process")]
    private async Task ProcessEvent(string eventName, string message, ulong deliveryTag)
    {
        try
        {
            if (!_subscriptionManager.HasSubscriptionsForEvent(eventName))
            {
                if (_channel != null)
                {
                    await _channel.BasicAckAsync(deliveryTag, false).ConfigureAwait(false);
                }

                return;
            }

            var eventType = _subscriptionManager.GetEventTypeByName(eventName);
            var integrationEvent = JsonSerializer.Deserialize(message, eventType);
            if (integrationEvent is null)
            {
                _logDeserializeWarning(_logger, eventName, null);
                if (_channel != null)
                {
                    await _channel.BasicAckAsync(deliveryTag, false).ConfigureAwait(false);
                }

                return;
            }

            await ExecuteWithRetriesAsync(async () =>
            {
                var handlers = _subscriptionManager.GetHandlersForEvent(eventName);
                foreach (var handlerType in handlers)
                {
                    var handler = _serviceProvider.GetService(handlerType);
                    if (handler == null)
                    {
                        continue;
                    }

                    var concreteType = typeof(IIntegrationEventConsumer<>).MakeGenericType(eventType);
                    var handleMethod = concreteType.GetMethod("Handle");
                    if (handleMethod == null)
                    {
                        continue;
                    }

                    var result = handleMethod.Invoke(handler, new object[] { integrationEvent });
                    if (result is Task task)
                    {
                        await task.ConfigureAwait(false);
                    }
                    else if (result is ValueTask vt)
                    {
                        await vt.AsTask().ConfigureAwait(false);
                    }
                }
            }).ConfigureAwait(false);

            if (_channel != null)
            {
                await _channel.BasicAckAsync(deliveryTag, false).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logProcessError(_logger, eventName, ex);
            if (_channel != null)
            {
                await _channel.BasicNackAsync(deliveryTag, false, false).ConfigureAwait(false);
            }
        }
    }

    private async Task ExecuteWithRetriesAsync(Func<Task> action)
    {
        var retries = Math.Max(1, _settings.RetryCount);
        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                await action().ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (attempt < retries)
            {
                var backoffSeconds = Math.Pow(2, attempt);
                _logRetry(_logger, ex.Message, backoffSeconds, ex);
                await Task.Delay(TimeSpan.FromSeconds(backoffSeconds)).ConfigureAwait(false);
            }
        }

        throw new Exception("Operation failed after retries");
    }

    // Internal test hook
    internal Task ProcessEventForTest(string eventName, string message, ulong deliveryTag) =>
        ProcessEvent(eventName, message, deliveryTag);

    internal async Task InitializeChannelForTest() =>
        _channel ??= await _rabbitMqConnection.CreateChannelAsync().ConfigureAwait(false);
}
