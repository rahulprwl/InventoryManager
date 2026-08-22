using InventoryHold.Contracts.DTO;
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
        return document.Id.ToString();
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

    private sealed class HoldDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public required InventoryHold.Contracts.Model.Hold Hold { get; set; }

        public Guid ItemUuid { get; set; }

        public int Quantity { get; set; }
    }
}