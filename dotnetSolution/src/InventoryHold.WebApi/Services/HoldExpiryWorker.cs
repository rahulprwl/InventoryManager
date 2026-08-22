using System.Text.Json;
using InventoryHold.Contracts.DTO;
using InventoryHold.Domain.Services;
using InventoryHold.Infrastructure.RabbitMq;

namespace InventoryHold.WebApi.Services;

public sealed class HoldExpiryWorker(
    IRabbitMqConnector connector,
    IServiceScopeFactory scopeFactory,
    ILogger<HoldExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await connector.ConsumeExpiredAsync(HandleMessageAsync, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task HandleMessageAsync(string message, CancellationToken cancellationToken)
    {
        var hold = JsonSerializer.Deserialize<HoldDto>(message);
        if (hold is null || hold.Hold.TransactionId == Guid.Empty)
        {
            logger.LogWarning("Ignoring invalid hold expiry message.");
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var holdService = scope.ServiceProvider.GetRequiredService<IHoldService>();
        await holdService.ExpireHoldAsync(hold.Hold.TransactionId, cancellationToken);
    }
}