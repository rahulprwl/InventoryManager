using InventoryHold.Contracts.DTO;

namespace InventoryHold.Domain.Adapters;

public interface IHoldMessageAdapter
{
    Task PublishExpiryAsync(
        HoldDto hold,
        CancellationToken cancellationToken = default);

    Task PublishStatusAsync(
        HoldDto hold,
        CancellationToken cancellationToken = default);
}