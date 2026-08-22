namespace InventoryHold.Domain.Adapters;

public interface IHoldLockAdapter
{
    Task<string?> AcquireAsync(
        Guid transactionId,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);

    Task<bool> ReleaseAsync(
        Guid transactionId,
        string token,
        CancellationToken cancellationToken = default);
}