using InventoryHold.Contracts.Model;

namespace InventoryHold.Contracts.DTO;

public class HoldDto
{
	public string? HoldId { get; set; }

	public required Hold Hold { get; set; }

	public required Guid ItemUuid { get; set; }
    
    public required int Quantity { get; set; }
}
