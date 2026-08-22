using System.Text.Json;
using InventoryHold.Contracts.DTO;
using InventoryHold.Domain.Adapters;

namespace InventoryHold.Infrastructure.RabbitMq;

public sealed class HoldMessageAdapter(IRabbitMqConnector connector) : IHoldMessageAdapter
{
    public Task PublishExpiryAsync(HoldDto hold, CancellationToken cancellationToken = default)
    {
        return connector.PublishAsync(
            RabbitMqConnector.HoldExchange,
            "hold.waiting",
            JsonSerializer.Serialize(hold),
            cancellationToken);
    }

    public Task PublishStatusAsync(HoldDto hold, CancellationToken cancellationToken = default)
    {
        return connector.PublishAsync(
            RabbitMqConnector.EventsExchange,
            $"hold.status.{hold.Hold.Status.ToString().ToLowerInvariant()}",
            JsonSerializer.Serialize(hold),
            cancellationToken);
    }
}