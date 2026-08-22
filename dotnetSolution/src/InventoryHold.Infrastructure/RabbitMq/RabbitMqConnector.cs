using System.Globalization;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InventoryHold.Infrastructure.RabbitMq;

public sealed class RabbitMqConnector : IRabbitMqConnector, IAsyncDisposable
{
    public const string HoldExchange = "inventory.hold.topic";
    public const string WaitingQueue = "inventory.hold.waiting.queue";
    public const string DeadLetterExchange = "inventory.hold.dlx.topic";
    public const string ExpiredQueue = "inventory.hold.expired.queue";
    public const string EventsExchange = "inventory.events.topic";

    private readonly ConnectionFactory connectionFactory;
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private IConnection? connection;
    private IChannel? channel;

    public RabbitMqConnector(ConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task SendMessageAsync(
        string queueName,
        string message,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(message);
        ValidateTtl(ttl);

        IChannel messageChannel = await GetChannelAsync(cancellationToken);
        await messageChannel.QueueDeclareAsync(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        BasicProperties? properties = ttl is null
            ? null
            : new BasicProperties
            {
                Expiration = ((long)ttl.Value.TotalMilliseconds)
                    .ToString(CultureInfo.InvariantCulture)
            };

        await messageChannel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(message),
            cancellationToken: cancellationToken);
    }

    public async Task EnsureHoldTopologyAsync(CancellationToken cancellationToken = default)
    {
        var messageChannel = await GetChannelAsync(cancellationToken);
        await messageChannel.ExchangeDeclareAsync(HoldExchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
        await messageChannel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
        await messageChannel.ExchangeDeclareAsync(EventsExchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
        await messageChannel.QueueDeclareAsync(
            WaitingQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = 900000,
                ["x-dead-letter-exchange"] = DeadLetterExchange,
                ["x-dead-letter-routing-key"] = "hold.expired"
            }, cancellationToken: cancellationToken);
        await messageChannel.QueueDeclareAsync(ExpiredQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await messageChannel.QueueBindAsync(WaitingQueue, HoldExchange, "hold.waiting", cancellationToken: cancellationToken);
        await messageChannel.QueueBindAsync(ExpiredQueue, DeadLetterExchange, "hold.expired", cancellationToken: cancellationToken);
    }

    public async Task PublishAsync(
        string exchangeName,
        string routingKey,
        string message,
        CancellationToken cancellationToken = default)
    {
        var messageChannel = await GetChannelAsync(cancellationToken);
        await messageChannel.BasicPublishAsync<BasicProperties>(
            exchangeName,
            routingKey,
            mandatory: false,
            basicProperties: new BasicProperties(),
            body: Encoding.UTF8.GetBytes(message),
            cancellationToken: cancellationToken);
    }

    public async Task ConsumeExpiredAsync(
        Func<string, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        await EnsureHoldTopologyAsync(cancellationToken);
        var messageChannel = await GetChannelAsync(cancellationToken);
        var consumer = new AsyncEventingBasicConsumer(messageChannel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            await handler(message, cancellationToken);
            await messageChannel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken);
        };
        await messageChannel.BasicConsumeAsync(ExpiredQueue, autoAck: false, consumer, cancellationToken);
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (channel is not null)
        {
            await channel.DisposeAsync();
        }

        if (connection is not null)
        {
            await connection.DisposeAsync();
        }

        initializationLock.Dispose();
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (channel is not null)
        {
            return channel;
        }

        await initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (channel is null)
            {
                connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
                channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            }

            return channel;
        }
        finally
        {
            initializationLock.Release();
        }
    }

    private static void ValidateTtl(TimeSpan? ttl)
    {
        if (ttl is not null && ttl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "TTL must be greater than zero.");
        }
    }
}
