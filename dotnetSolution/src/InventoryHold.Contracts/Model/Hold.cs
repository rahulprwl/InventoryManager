using InventoryHold.Contracts.Enums;

namespace InventoryHold.Contracts.Model;

public class Hold
{
	public required string UserName { get; set; }

	public DateTime StartTime { get; set; }

	public TimeSpan TTL { get; set; }

	public Guid TransactionId { get; set; }

	public HoldStatus Status { get; set; } = HoldStatus.Active;

	public DateTime ExpiresAt { get; set; }

	public DateTime UpdatedAt { get; set; }
}
