using InventoryHold.Contracts.DTO;
using InventoryHold.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryHold.WebApi.Controller;

[ApiController]
[Route("api/holds")]
public sealed class HoldController(IHoldService holdService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] HoldDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var holdId = await holdService.CreateHoldAsync(request, cancellationToken);
            return Created($"api/holds/{request.Hold.TransactionId}", new { holdId, request.Hold.TransactionId });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpPost("{holdId}/complete")]
    public async Task<IActionResult> Complete(string holdId, CancellationToken cancellationToken)
    {
        var changed = await holdService.CompleteHoldAsync(holdId, cancellationToken);
        return changed ? Ok() : Conflict("Hold is not active or was not found.");
    }

    [HttpDelete("{holdId}")]
    public async Task<IActionResult> Release(string holdId, CancellationToken cancellationToken)
    {
        var changed = await holdService.ReleaseHoldAsync(holdId, cancellationToken);
        return changed ? NoContent() : Conflict("Hold is not active or was not found.");
    }
}