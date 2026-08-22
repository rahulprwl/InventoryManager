using StackExchange.Redis;

namespace InventoryHold.Infrastructure.Redis;

public interface IRedisRepository
{
    Task<bool> AddKeyWithTtlAsync(
        string key,
        RedisValue value,
        TimeSpan ttl,
        CommandFlags flags = CommandFlags.None);

    Task<string> GetValueAsync(string key, CommandFlags flags = CommandFlags.None);
}