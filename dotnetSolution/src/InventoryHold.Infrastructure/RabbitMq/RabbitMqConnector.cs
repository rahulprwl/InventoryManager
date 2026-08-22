using System.Globalization;
using System.Text;
using RabbitMQ.Client;

namespace InventoryHold.Infrastructure.RabbitMq;

public sealed class RabbitMqConnector : IRabbitMqConnector, IAsyncDisposable
{
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
