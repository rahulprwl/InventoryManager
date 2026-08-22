using InventoryHold.Contracts.Enums;

namespace InventoryHold.Contracts.DTO;

public sealed record HoldSummaryDto(
    string HoldId,
    Guid TransactionId,
    string UserName,
    Guid ItemUuid,
    int Quantity,
    DateTime StartTime,
    DateTime ExpiresAt,
    HoldStatus CachedStatus,
    HoldStatus Status);