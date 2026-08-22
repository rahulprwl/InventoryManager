using InventoryHold.Contracts.DTO;

namespace InventoryHold.Domain.Services;

public interface IItemService
{
    Task<Item> AddItemAsync(Item item, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Item>> GetItemsAsync(
        int offset,
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<Item?> UpdateStockAsync(
        Guid itemUuid,
        int stock,
        string updatedBy,
        CancellationToken cancellationToken = default);

    Task<Item?> DecrementStockAsync(
        Guid itemUuid,
        int amount,
        string updatedBy,
        CancellationToken cancellationToken = default);

    Task<Item?> IncrementStockAsync(
        Guid itemUuid,
        int amount,
        string updatedBy,
        CancellationToken cancellationToken = default);
}