namespace InventoryHold.Contracts.DTO;

public class Item
{
	public Guid? Uuid { get; set; }

	public DateTime? Expiry { get; set; }

	public required string Name { get; set; }

	public int CurrentStock { get; set; }

	public DateTime LastUpdatedTime { get; set; }

	public required string LastUpdatedBy { get; set; }

	public required string Category { get; set; }

	public required string Brand { get; set; }
}
