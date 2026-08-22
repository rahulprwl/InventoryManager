using InventoryHold.Contracts.DTO;

namespace InventoryHold.Domain.Services;

public interface IHoldService
{
    Task<IReadOnlyList<HoldSummaryDto>> GetAllHoldsAsync(
        CancellationToken cancellationToken = default);

    Task<string> CreateHoldAsync(HoldDto hold, CancellationToken cancellationToken = default);

    Task<bool> CompleteHoldAsync(string holdId, CancellationToken cancellationToken = default);

    Task<bool> ReleaseHoldAsync(string holdId, CancellationToken cancellationToken = default);

    Task ExpireHoldAsync(Guid transactionId, CancellationToken cancellationToken = default);
}