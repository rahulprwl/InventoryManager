using InventoryHold.Domain.Adapters;

namespace InventoryHold.Infrastructure.Redis;

public sealed class HoldLockAdapter(IRedisRepository repository) : IHoldLockAdapter
{
    public Task<string?> AcquireAsync(Guid transactionId, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var token = Guid.NewGuid().ToString("N");
        return AcquireCoreAsync(transactionId, token, lifetime);
    }

    public Task<bool> ReleaseAsync(Guid transactionId, string token, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return repository.ReleaseLockAsync($"lock:hold:{transactionId}", token);
    }

    private async Task<string?> AcquireCoreAsync(Guid transactionId, string token, TimeSpan lifetime)
    {
        return await repository.AcquireLockAsync($"lock:hold:{transactionId}", token, lifetime) ? token : null;
    }
}