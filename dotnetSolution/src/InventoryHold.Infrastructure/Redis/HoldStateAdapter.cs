using InventoryHold.Contracts.Enums;
using InventoryHold.Contracts.Model;
using InventoryHold.Domain.Adapters;

namespace InventoryHold.Infrastructure.Redis;

public sealed class HoldStateAdapter(IRedisRepository repository) : IHoldStateAdapter
{
    public async Task<IReadOnlyDictionary<Guid, HoldStatus>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, HoldStatus>();
        var keys = await repository.FindKeysAsync("hold:*:state", cancellationToken);
        foreach (var key in keys)
        {
            var parts = key.Split(':');
            if (parts.Length != 3 || !Guid.TryParse(parts[1], out var transactionId))
            {
                continue;
            }

            var value = await repository.GetNullableValueAsync(key);
            if (Enum.TryParse<HoldStatus>(value, out var status))
            {
                result[transactionId] = status;
            }
        }

        return result;
    }

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