using FluentAssertions;
using InventoryHold.Contracts.DTO;
using InventoryHold.Domain.Repositories;
using InventoryHold.Domain.Services;
using Moq;

namespace InventoryHold.UnitTests;

public class ItemServiceTests
{
    private readonly Mock<IItemRepository> itemRepository = new();
    private readonly ItemService service;

    public ItemServiceTests()
    {
        service = new ItemService(itemRepository.Object);
    }

    [Fact]
    public async Task AddItemAsync_ShouldValidateAndDelegateToRepository()
    {
        var item = CreateItem();
        itemRepository
            .Setup(repository => repository.AddItemAsync(item, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await service.AddItemAsync(item);

        result.Should().BeSameAs(item);
        itemRepository.Verify(
            repository => repository.AddItemAsync(item, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetItemsAsync_ShouldDelegatePagingToRepository()
    {
        var items = new[] { CreateItem() };
        itemRepository
            .Setup(repository => repository.GetItemsAsync(5, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var result = await service.GetItemsAsync(5, 20);

        result.Should().ContainSingle().Which.Should().BeSameAs(items[0]);
        itemRepository.Verify(
            repository => repository.GetItemsAsync(5, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStockAsync_ShouldDelegateToRepository()
    {
        var itemUuid = Guid.NewGuid();
        var updatedItem = CreateItem(itemUuid);
        itemRepository
            .Setup(repository => repository.UpdateStockAsync(
                itemUuid,
                12,
                "tester",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedItem);

        var result = await service.UpdateStockAsync(itemUuid, 12, "tester");

        result.Should().BeSameAs(updatedItem);
        itemRepository.Verify(
            repository => repository.UpdateStockAsync(
                itemUuid,
                12,
                "tester",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IncrementStockAsync_ShouldDelegateToRepository()
    {
        var itemUuid = Guid.NewGuid();
        var updatedItem = CreateItem(itemUuid);
        itemRepository
            .Setup(repository => repository.IncrementStockAsync(
                itemUuid,
                3,
                "tester",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedItem);

        var result = await service.IncrementStockAsync(itemUuid, 3, "tester");

        result.Should().BeSameAs(updatedItem);
        itemRepository.Verify(
            repository => repository.IncrementStockAsync(
                itemUuid,
                3,
                "tester",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DecrementStockAsync_ShouldDelegateToRepository()
    {
        var itemUuid = Guid.NewGuid();
        var updatedItem = CreateItem(itemUuid);
        itemRepository
            .Setup(repository => repository.DecrementStockAsync(
                itemUuid,
                2,
                "tester",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedItem);

        var result = await service.DecrementStockAsync(itemUuid, 2, "tester");

        result.Should().BeSameAs(updatedItem);
        itemRepository.Verify(
            repository => repository.DecrementStockAsync(
                itemUuid,
                2,
                "tester",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_ShouldRejectInvalidItem()
    {
        var item = CreateItem();
        item.Name = " ";

        Func<Task> action = () => service.AddItemAsync(item);

        await action.Should().ThrowAsync<ArgumentException>();
        itemRepository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, 0)]
    public async Task GetItemsAsync_ShouldRejectInvalidPaging(int offset, int limit)
    {
        Func<Task> action = () => service.GetItemsAsync(offset, limit);

        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
        itemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateStockAsync_ShouldRejectNegativeStock()
    {
        Func<Task> action = () => service.UpdateStockAsync(Guid.NewGuid(), -1, "tester");

        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
        itemRepository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task StockChangeAsync_ShouldRejectNonPositiveAmount(int amount)
    {
        Func<Task> action = () => service.IncrementStockAsync(Guid.NewGuid(), amount, "tester");

        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
        itemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StockChangeAsync_ShouldRejectEmptyItemUuid()
    {
        Func<Task> action = () => service.DecrementStockAsync(Guid.Empty, 1, "tester");

        await action.Should().ThrowAsync<ArgumentException>();
        itemRepository.VerifyNoOtherCalls();
    }

    private static Item CreateItem(Guid? itemUuid = null)
    {
        return new Item
        {
            Uuid = itemUuid ?? Guid.NewGuid(),
            Name = "Keyboard",
            Category = "Peripherals",
            Brand = "Example",
            CurrentStock = 10,
            LastUpdatedBy = "seed",
            LastUpdatedTime = DateTime.UtcNow
        };
    }
}