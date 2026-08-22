using InventoryHold.Contracts.DTO;

namespace InventoryHold.Domain.Repositories;

public interface IHoldRepository
{
    Task<string> CreateHoldAsync(
        HoldDto hold,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteHoldAsync(
        string holdId,
        CancellationToken cancellationToken = default);
}