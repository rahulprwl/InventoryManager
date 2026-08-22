using InventoryHold.Contracts.DTO;
using InventoryHold.Contracts.Enums;
using InventoryHold.Contracts.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace InventoryHold.Infrastructure.Mongo;

public sealed class HoldMongoRepository : IHoldMongoRepository
{
    private readonly IMongoCollection<HoldDocument> holds;

    public HoldMongoRepository(IMongoDatabase database, string collectionName = "holds")
    {
        holds = database.GetCollection<HoldDocument>(collectionName);
    }

    public async Task<string> CreateHoldAsync(
        HoldDto hold,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hold);

        var document = new HoldDocument
        {
            Hold = hold.Hold,
            ItemUuid = hold.ItemUuid,
            Quantity = hold.Quantity
        };

        await holds.InsertOneAsync(document, cancellationToken: cancellationToken);
        hold.HoldId = document.Id.ToString();
        return hold.HoldId;
    }

    public async Task<bool> DeleteHoldAsync(
        string holdId,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(holdId, out ObjectId objectId))
        {
            return false;
        }

        var result = await holds.DeleteOneAsync(hold => hold.Id == objectId, cancellationToken);
        return result.DeletedCount == 1;
    }

    public async Task<HoldDto?> GetHoldAsync(
        string holdId,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(holdId, out var objectId))
        {
            return null;
        }

        var document = await holds.Find(hold => hold.Id == objectId)
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDto(document);
    }

    public async Task<HoldDto?> GetHoldByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var document = await holds.Find(hold => hold.Hold.TransactionId == transactionId)
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDto(document);
    }

    public async Task<IReadOnlyList<HoldDto>> GetHoldsAsync(
        IEnumerable<Guid> transactionIds,
        CancellationToken cancellationToken = default)
    {
        var ids = transactionIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Array.Empty<HoldDto>();
        }

        var documents = await holds.Find(
                Builders<HoldDocument>.Filter.In(document => document.Hold.TransactionId, ids))
            .ToListAsync(cancellationToken);
        return documents.Select(ToDto).ToArray();
    }

    public async Task<bool> UpdateStatusIfActiveAsync(
        string holdId,
        HoldStatus status,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(holdId, out var objectId))
        {
            return false;
        }

        var update = Builders<HoldDocument>.Update
            .Set(hold => hold.Hold.Status, status)
            .Set(hold => hold.Hold.UpdatedAt, DateTime.UtcNow);
        var result = await holds.UpdateOneAsync(
            hold => hold.Id == objectId && hold.Hold.Status == HoldStatus.Active,
            update,
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<bool> UpdateStatusAsync(
        string holdId,
        HoldStatus status,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(holdId, out var objectId))
        {
            return false;
        }

        var update = Builders<HoldDocument>.Update
            .Set(hold => hold.Hold.Status, status)
            .Set(hold => hold.Hold.UpdatedAt, DateTime.UtcNow);
        var result = await holds.UpdateOneAsync(
            hold => hold.Id == objectId,
            update,
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    private static HoldDto ToDto(HoldDocument document) => new()
    {
        HoldId = document.Id.ToString(),
        Hold = document.Hold,
        ItemUuid = document.ItemUuid,
        Quantity = document.Quantity
    };

    private sealed class HoldDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public required InventoryHold.Contracts.Model.Hold Hold { get; set; }

        public Guid ItemUuid { get; set; }

        public int Quantity { get; set; }
    }
}