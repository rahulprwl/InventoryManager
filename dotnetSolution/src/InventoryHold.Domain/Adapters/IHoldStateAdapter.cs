using InventoryHold.Contracts.Enums;
using InventoryHold.Contracts.Model;

namespace InventoryHold.Domain.Adapters;

public interface IHoldStateAdapter
{
    Task<HoldStatus?> GetAsync(Guid transactionId, CancellationToken cancellationToken = default);

    Task SetAsync(
        Guid transactionId,
        HoldStatus status,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);
}