namespace InventoryHold.Infrastructure.RabbitMq;

public interface IRabbitMqConnector
{
    Task EnsureHoldTopologyAsync(CancellationToken cancellationToken = default);

    Task PublishAsync(
        string exchangeName,
        string routingKey,
        string message,
        CancellationToken cancellationToken = default);

    Task ConsumeExpiredAsync(
        Func<string, CancellationToken, Task> handler,
        CancellationToken cancellationToken);

    Task SendMessageAsync(
        string queueName,
        string message,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);
}
