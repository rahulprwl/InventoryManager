using InventoryHold.Contracts.DTO;
using MongoDB.Driver;

namespace InventoryHold.Infrastructure.Mongo;

public sealed class InventoryMongoRepository : IInventoryMongoRepository
{
    private readonly IMongoCollection<Item> items;

    public InventoryMongoRepository(IMongoDatabase database, string collectionName = "items")
    {
        items = database.GetCollection<Item>(collectionName);
    }

    public async Task<Item> AddItemAsync(Item item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Uuid == Guid.Empty)
        {
            item.Uuid = Guid.NewGuid();
        }

        await items.InsertOneAsync(item, cancellationToken: cancellationToken);
        return item;
    }

    public async Task<Item?> UpdateStockAsync(
        Guid itemUuid,
        int stock,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        var update = Builders<Item>.Update
            .Set(item => item.CurrentStock, stock)
            .Set(item => item.LastUpdatedBy, updatedBy)
            .Set(item => item.LastUpdatedTime, DateTime.UtcNow);

        return await items.FindOneAndUpdateAsync(
            item => item.Uuid == itemUuid,
            update,
            new FindOneAndUpdateOptions<Item> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
    }

    public Task<Item?> DecrementStockAsync(
        Guid itemUuid,
        int amount,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        ValidateAmount(amount);
        return ChangeStockAsync(itemUuid, -amount, updatedBy, cancellationToken);
    }

    public Task<Item?> IncrementStockAsync(
        Guid itemUuid,
        int amount,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        ValidateAmount(amount);
        return ChangeStockAsync(itemUuid, amount, updatedBy, cancellationToken);
    }

    private async Task<Item?> ChangeStockAsync(
        Guid itemUuid,
        int amount,
        string updatedBy,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        var update = Builders<Item>.Update
            .Inc(item => item.CurrentStock, amount)
            .Set(item => item.LastUpdatedBy, updatedBy)
            .Set(item => item.LastUpdatedTime, DateTime.UtcNow);

        return await items.FindOneAndUpdateAsync(
            item => item.Uuid == itemUuid,
            update,
            new FindOneAndUpdateOptions<Item> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
    }

    private static void ValidateAmount(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amount, 0);
    }
}