using InventoryHold.Contracts.DTO;
using InventoryHold.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryHold.WebApi.Controller;

[ApiController]
[Route("api/items")]
public sealed class ItemController(IItemService itemService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(IReadOnlyList<Item>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BatchCreateItemsResponse), StatusCodes.Status207MultiStatus)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItems(
        [FromBody] IReadOnlyList<Item> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return BadRequest("At least one item is required.");
        }

        var createdItems = new List<Item>(items.Count);
        var failures = new List<ItemCreationFailure>();

        for (var index = 0; index < items.Count; index++)
        {
            try
            {
                createdItems.Add(await itemService.AddItemAsync(items[index], cancellationToken));
            }
            catch (ArgumentException exception)
            {
                failures.Add(new ItemCreationFailure(index, exception.Message));
            }
        }

        if (failures.Count == 0)
        {
            return StatusCode(StatusCodes.Status201Created, createdItems);
        }

        if (createdItems.Count == 0)
        {
            return BadRequest(new BatchCreateItemsResponse(createdItems, failures));
        }

        return StatusCode(
            StatusCodes.Status207MultiStatus,
            new BatchCreateItemsResponse(createdItems, failures));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Item>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetItems(
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (offset < 0 || limit <= 0)
        {
            return BadRequest("Offset must be non-negative and limit must be greater than zero.");
        }

        var items = await itemService.GetItemsAsync(offset, limit, cancellationToken);
        return items.Count == 0 ? NotFound() : Ok(items);
    }

    [HttpPut("{itemUuid:guid}/stock")]
    [ProducesResponseType(typeof(Item), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStock(
        Guid itemUuid,
        [FromBody] StockUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Stock < 0 || string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            return BadRequest("Stock must be non-negative and updatedBy is required.");
        }

        var item = await itemService.UpdateStockAsync(
            itemUuid,
            request.Stock,
            request.UpdatedBy,
            cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }
}

public sealed record StockUpdateRequest(int Stock, string UpdatedBy);

public sealed record BatchCreateItemsResponse(
    IReadOnlyList<Item> CreatedItems,
    IReadOnlyList<ItemCreationFailure> Failures);

public sealed record ItemCreationFailure(int Index, string Error);