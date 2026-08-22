using FluentAssertions;
using InventoryHold.Contracts.DTO;
using InventoryHold.Contracts.Enums;
using InventoryHold.Contracts.Model;
using InventoryHold.Domain.Adapters;
using InventoryHold.Domain.Repositories;
using InventoryHold.Domain.Services;
using Moq;

namespace InventoryHold.UnitTests;

public sealed class HoldServiceTests
{
    [Fact]
    public async Task GetAllHoldsAsync_ShouldCombineCachedIdsWithDatabaseStatus()
    {
        var transactionId = Guid.NewGuid();
        var stateAdapter = new Mock<IHoldStateAdapter>();
        var holdRepository = new Mock<IHoldRepository>();
        var service = CreateService(holdRepository, stateAdapter);
        var hold = new HoldDto
        {
            Hold = new Hold
            {
                UserName = "tester",
                TransactionId = transactionId,
                Status = HoldStatus.Completed,
                StartTime = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            },
            ItemUuid = Guid.NewGuid(),
            Quantity = 2
        };

        stateAdapter
            .Setup(adapter => adapter.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, HoldStatus>
            {
                [transactionId] = HoldStatus.Active
            });
        holdRepository
            .Setup(repository => repository.GetHoldsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Single() == transactionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { hold });

        var result = await service.GetAllHoldsAsync();

        result.Should().ContainSingle().Which.Should().Match<HoldSummaryDto>(summary =>
            summary.HoldId == hold.HoldId &&
            summary.TransactionId == transactionId &&
            summary.CachedStatus == HoldStatus.Active &&
            summary.Status == HoldStatus.Completed);
    }

    private static HoldService CreateService(
        Mock<IHoldRepository> holdRepository,
        Mock<IHoldStateAdapter> stateAdapter)
    {
        return new HoldService(
            holdRepository.Object,
            Mock.Of<IItemRepository>(),
            stateAdapter.Object,
            Mock.Of<IHoldLockAdapter>(),
            Mock.Of<IHoldMessageAdapter>());
    }
}