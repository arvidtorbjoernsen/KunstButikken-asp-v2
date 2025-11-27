using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using KunstButikken.IntegrationEvents.Contracts;
using KunstButikken.IntegrationEvents.Contracts.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

// Updated namespace

namespace KunstButikken.AdminService.IntegrationEvents;

internal sealed class RabbitMqEventBus : IEventBus, IDisposable
{
    private static readonly Action<ILogger<RabbitMqEventBus>, string, double, Exception?> LogRetry
        = LoggerMessage.Define<string, double>(LogLevel.Warning, new EventId(1000, nameof(LogRetry)),
            "Could not process event: {Message}. Retrying in {Seconds}s");

    private static readonly Action<ILogger<RabbitMqEventBus>, string, Exception?> LogPublished
        = LoggerMessage.Define<string>(LogLevel.Information, new EventId(1001, nameof(LogPublished)),
            "Published event {EventName}");

    private static readonly Action<ILogger<RabbitMqEventBus>, string, Exception?> LogDeserializeWarning
        = LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1002, nameof(LogDeserializeWarning)),
            "Failed to deserialize message for event {EventName}");

    private static readonly Action<ILogger<RabbitMqEventBus>, string, Exception?> LogProcessError
        = LoggerMessage.Define<string>(LogLevel.Error, new EventId(1003, nameof(LogProcessError)),
            "Error processing event {EventName} after retries.");

    private readonly SemaphoreSlim _channelLock = new(1, 1);
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly IConnection _rabbitMqConnection;
    private readonly int _retryCount;
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
        _settings = options.Value ??
                    throw new ArgumentException("RabbitMqSettings missing in options", nameof(options));
        _subscriptionManager = subscriptionManager;
        _serviceProvider = serviceProvider;

        // Use configured retry count; default to 3 if not set or invalid
        _retryCount = Math.Max(0, _settings.RetryCount);
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
            await _channel.ExchangeDeclareAsync(_settings.ExchangeName, ExchangeType.Fanout, true, false,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var json = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());
            var body = Encoding.UTF8.GetBytes(json);

            await _channel.BasicPublishAsync(_settings.ExchangeName, eventName, body, cancellationToken).ConfigureAwait(false);
            LogPublished(_logger, eventName, null);
        }
        finally
        {
            _channelLock.Release();
        }
    }

    public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventConsumer<T> =>
        _ = SubscribeAsync<T, TH>();

    public async Task SubscribeAsync<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventConsumer<T>
    {
        var eventName = typeof(T).Name;
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
                    // swallow to keep the consumer alive; ProcessEvent will nack on failure
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

        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Event processing must catch all exceptions to nack messages and avoid crashing the process")]
    private async Task ProcessEvent(string eventName, string message, ulong deliveryTag)
    {
        ArgumentNullException.ThrowIfNull(eventName);
        ArgumentNullException.ThrowIfNull(message);

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
                LogDeserializeWarning(_logger, eventName, null);
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

                    var result = handleMethod.Invoke(handler, new[] { integrationEvent });
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
            LogProcessError(_logger, eventName, ex);
            if (_channel != null)
            {
                await _channel.BasicNackAsync(deliveryTag, false, false).ConfigureAwait(false);
            }
        }
    }

    private async Task ExecuteWithRetriesAsync(Func<Task> action)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                await action().ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                attempt++;
                if (attempt > _retryCount)
                {
                    throw;
                }

                // Exponential backoff: 2^attempt seconds
                var delaySeconds = Math.Pow(2, attempt);
                LogRetry(_logger, ex.Message, delaySeconds, ex);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds)).ConfigureAwait(false);
            }
        }
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

    // Internal test hook
    internal Task ProcessEventForTest(string eventName, string message, ulong deliveryTag) =>
        ProcessEvent(eventName, message, deliveryTag);

    // Internal test hook to initialize the channel (sync CreateModel from RabbitMQ.Client)
    internal async Task InitializeChannelForTest() =>
        _channel ??= await _rabbitMqConnection.CreateChannelAsync().ConfigureAwait(false);
}
