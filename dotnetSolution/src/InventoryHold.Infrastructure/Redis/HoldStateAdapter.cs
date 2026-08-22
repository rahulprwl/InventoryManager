using InventoryHold.Contracts.Enums;
using InventoryHold.Contracts.Model;
using InventoryHold.Domain.Adapters;

namespace InventoryHold.Infrastructure.Redis;

public sealed class HoldStateAdapter(IRedisRepository repository) : IHoldStateAdapter
{
    public async Task<HoldStatus?> GetAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await repository.GetNullableValueAsync($"hold:{transactionId}:state");
        return Enum.TryParse<HoldStatus>(value, out var status) ? status : null;
    }

    public Task SetAsync(Guid transactionId, HoldStatus status, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return repository.SetValueAsync($"hold:{transactionId}:state", status.ToString(), lifetime);
    }
}