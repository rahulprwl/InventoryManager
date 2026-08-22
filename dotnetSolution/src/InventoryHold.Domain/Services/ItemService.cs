using InventoryHold.Contracts.DTO;
using InventoryHold.Domain.Repositories;

namespace InventoryHold.Domain.Services;

public sealed class ItemService(IItemRepository itemRepository) : IItemService
{
    public async Task<Item> AddItemAsync(
        Item item,
        CancellationToken cancellationToken = default)
    {
        ValidateItem(item);
        return await itemRepository.AddItemAsync(item, cancellationToken);
    }

    public Task<IReadOnlyList<Item>> GetItemsAsync(
        int offset,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(limit, 0);

        return itemRepository.GetItemsAsync(offset, limit, cancellationToken);
    }

    public Task<Item?> UpdateStockAsync(
        Guid itemUuid,
        int stock,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        ValidateItemUuid(itemUuid);
        ArgumentOutOfRangeException.ThrowIfNegative(stock);
        ValidateUpdatedBy(updatedBy);

        return itemRepository.UpdateStockAsync(itemUuid, stock, updatedBy, cancellationToken);
    }

    public Task<Item?> DecrementStockAsync(
        Guid itemUuid,
        int amount,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        ValidateStockChange(itemUuid, amount, updatedBy);
        return itemRepository.DecrementStockAsync(itemUuid, amount, updatedBy, cancellationToken);
    }

    public Task<Item?> IncrementStockAsync(
        Guid itemUuid,
        int amount,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        ValidateStockChange(itemUuid, amount, updatedBy);
        return itemRepository.IncrementStockAsync(itemUuid, amount, updatedBy, cancellationToken);
    }

    private static void ValidateItem(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Category);
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Brand);
        ValidateUpdatedBy(item.LastUpdatedBy);
        ArgumentOutOfRangeException.ThrowIfNegative(item.CurrentStock);
    }

    private static void ValidateStockChange(Guid itemUuid, int amount, string updatedBy)
    {
        ValidateItemUuid(itemUuid);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amount, 0);
        ValidateUpdatedBy(updatedBy);
    }

    private static void ValidateItemUuid(Guid itemUuid)
    {
        if (itemUuid == Guid.Empty)
        {
            throw new ArgumentException("Item UUID cannot be empty.", nameof(itemUuid));
        }
    }

    private static void ValidateUpdatedBy(string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);
    }
}