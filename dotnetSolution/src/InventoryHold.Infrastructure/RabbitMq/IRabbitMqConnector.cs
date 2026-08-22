namespace InventoryHold.Infrastructure.RabbitMq;

public interface IRabbitMqConnector
{
    Task SendMessageAsync(
        string queueName,
        string message,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);
}
