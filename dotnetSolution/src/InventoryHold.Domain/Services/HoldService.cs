using InventoryHold.Contracts.DTO;
using InventoryHold.Contracts.Enums;
using InventoryHold.Contracts.Model;
using InventoryHold.Domain.Adapters;
using InventoryHold.Domain.Repositories;

namespace InventoryHold.Domain.Services;

public sealed class HoldService(
    IHoldRepository holdRepository,
    IItemRepository itemRepository,
    IHoldStateAdapter stateAdapter,
    IHoldLockAdapter lockAdapter,
    IHoldMessageAdapter messageAdapter) : IHoldService
{
    private static readonly TimeSpan HoldLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(30);

    public async Task<string> CreateHoldAsync(HoldDto hold, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hold);
        ArgumentException.ThrowIfNullOrWhiteSpace(hold.Hold.UserName);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(hold.Quantity, 0);
        if (hold.ItemUuid == Guid.Empty)
        {
            throw new ArgumentException("Item UUID cannot be empty.", nameof(hold));
        }

        var item = await itemRepository.DecrementStockAsync(
            hold.ItemUuid,
            hold.Quantity,
            hold.Hold.UserName,
            cancellationToken);
        if (item is null)
        {
            throw new InvalidOperationException("Item was not found or does not have enough stock.");
        }

        hold.Hold.StartTime = DateTime.UtcNow;
        if (hold.Hold.TransactionId == Guid.Empty)
        {
            hold.Hold.TransactionId = Guid.NewGuid();
        }
        hold.Hold.TTL = HoldLifetime;
        hold.Hold.ExpiresAt = hold.Hold.StartTime.Add(HoldLifetime);
        hold.Hold.Status = HoldStatus.Active;
        hold.Hold.UpdatedAt = hold.Hold.StartTime;
        var holdId = await holdRepository.CreateHoldAsync(hold, cancellationToken);

        await stateAdapter.SetAsync(hold.Hold.TransactionId, HoldStatus.Active, CacheLifetime, cancellationToken);
        await messageAdapter.PublishExpiryAsync(hold, cancellationToken);
        await messageAdapter.PublishStatusAsync(hold, cancellationToken);
        return holdId;
    }

    public Task<bool> CompleteHoldAsync(string holdId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(holdId, HoldStatus.Completed, false, cancellationToken);

    public Task<bool> ReleaseHoldAsync(string holdId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(holdId, HoldStatus.Released, true, cancellationToken);

    public async Task ExpireHoldAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var token = await lockAdapter.AcquireAsync(transactionId, TimeSpan.FromSeconds(5), cancellationToken);
        if (token is null)
        {
            throw new InvalidOperationException("Hold is currently being changed; retry the message.");
        }

        try
        {
            var cachedStatus = await stateAdapter.GetAsync(transactionId, cancellationToken);
            if (cachedStatus is HoldStatus.Completed or HoldStatus.Released or HoldStatus.Expired)
            {
                return;
            }

            var hold = await FindByTransactionIdAsync(transactionId, cancellationToken);
            if (hold is null || hold.Hold.Status != HoldStatus.Active)
            {
                return;
            }

            if (!await holdRepository.UpdateStatusIfActiveAsync(
                    holdId: hold.Hold.TransactionId.ToString(), HoldStatus.Expired, cancellationToken))
            {
                return;
            }

            await itemRepository.IncrementStockAsync(
                hold.ItemUuid, hold.Quantity, hold.Hold.UserName, cancellationToken);
            hold.Hold.Status = HoldStatus.Expired;
            await stateAdapter.SetAsync(transactionId, HoldStatus.Expired, CacheLifetime, cancellationToken);
            await messageAdapter.PublishStatusAsync(hold, cancellationToken);
        }
        finally
        {
            await lockAdapter.ReleaseAsync(transactionId, token, cancellationToken);
        }
    }

    private async Task<bool> ChangeStatusAsync(
        string holdId,
        HoldStatus status,
        bool restoreStock,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(holdId, out var transactionId))
        {
            throw new ArgumentException("Hold ID must be a transaction UUID.", nameof(holdId));
        }

        var token = await lockAdapter.AcquireAsync(transactionId, TimeSpan.FromSeconds(5), cancellationToken);
        if (token is null)
        {
            throw new InvalidOperationException("Hold is currently being changed.");
        }

        try
        {
            var hold = await FindByTransactionIdAsync(transactionId, cancellationToken);
            if (hold is null || hold.Hold.Status != HoldStatus.Active)
            {
                return false;
            }

            if (!await holdRepository.UpdateStatusIfActiveAsync(holdId, status, cancellationToken))
            {
                return false;
            }

            if (restoreStock)
            {
                await itemRepository.IncrementStockAsync(
                    hold.ItemUuid, hold.Quantity, hold.Hold.UserName, cancellationToken);
            }

            hold.Hold.Status = status;
            await stateAdapter.SetAsync(transactionId, status, CacheLifetime, cancellationToken);
            await messageAdapter.PublishStatusAsync(hold, cancellationToken);
            return true;
        }
        finally
        {
            await lockAdapter.ReleaseAsync(transactionId, token, cancellationToken);
        }
    }

    private async Task<HoldDto?> FindByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        return await holdRepository.GetHoldAsync(transactionId.ToString(), cancellationToken);
    }
}