using InventoryHold.Contracts.DTO;
using InventoryHold.Contracts.Enums;
using InventoryHold.Contracts.Model;

namespace InventoryHold.Domain.Repositories;

public interface IHoldRepository
{
    Task<string> CreateHoldAsync(
        HoldDto hold,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteHoldAsync(
        string holdId,
        CancellationToken cancellationToken = default);

    Task<HoldDto?> GetHoldAsync(
        string holdId,
        CancellationToken cancellationToken = default);

    Task<HoldDto?> GetHoldByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HoldDto>> GetHoldsAsync(
        IEnumerable<Guid> transactionIds,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateStatusIfActiveAsync(
        string holdId,
        HoldStatus status,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateStatusAsync(
        string holdId,
        HoldStatus status,
        CancellationToken cancellationToken = default);
}