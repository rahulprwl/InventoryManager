namespace InventoryHold.Contracts.Model;

public class Hold
{
	public required string UserName { get; set; }

	public DateTime StartTime { get; set; }

	public TimeSpan TTL { get; set; }

	public Guid TransactionId { get; set; }
}
