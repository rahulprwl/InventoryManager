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

    Task SetValueAsync(string key, RedisValue value, TimeSpan ttl, CommandFlags flags = CommandFlags.None);

    Task<string?> GetNullableValueAsync(string key, CommandFlags flags = CommandFlags.None);

    Task<bool> AcquireLockAsync(string key, string token, TimeSpan ttl, CommandFlags flags = CommandFlags.None);

    Task<bool> ReleaseLockAsync(string key, string token, CommandFlags flags = CommandFlags.None);
}