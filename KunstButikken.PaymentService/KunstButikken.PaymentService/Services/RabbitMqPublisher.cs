using System.Text;
using RabbitMQ.Client;

namespace KunstButikken.PaymentService.Services;

/// <summary>
/// Simple helper to publish messages to RabbitMQ using the IConnection injected by Aspire.RabbitMQ.Client.
/// Uses the RabbitMQ.Client v7 IChannel/CreateChannelAsync API.
/// </summary>
public sealed class RabbitMqPublisher
{
    private readonly IConnection _connection;

    public RabbitMqPublisher(IConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public void Publish(string queueName, string message)
    {
        // Synchronous wrapper for convenience
        PublishAsync(queueName, message).GetAwaiter().GetResult();
    }

    public async Task PublishAsync(string queueName, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(queueName)) throw new ArgumentException("queueName must be set", nameof(queueName));
        if (message is null) throw new ArgumentNullException(nameof(message));

        var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        try
        {
            // Ensure queue exists
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null).ConfigureAwait(false);

            var body = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                body: body,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            try { await channel.CloseAsync().ConfigureAwait(false); } catch { }
            try { await channel.DisposeAsync().ConfigureAwait(false); } catch { }
        }
    }
}
