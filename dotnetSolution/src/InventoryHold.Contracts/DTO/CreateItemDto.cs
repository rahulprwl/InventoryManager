namespace InventoryHold.Contracts.DTO;

public sealed record CreateItemDto(
    DateTime? Expiry,
    string Name,
    int CurrentStock,
    DateTime LastUpdatedTime,
    string LastUpdatedBy,
    string Category,
    string Brand)
{
    public Item ToItem() => new()
    {
        Uuid = Guid.Empty,
        Expiry = Expiry,
        Name = Name,
        CurrentStock = CurrentStock,
        LastUpdatedTime = LastUpdatedTime,
        LastUpdatedBy = LastUpdatedBy,
        Category = Category,
        Brand = Brand
    };
}