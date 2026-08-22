using StackExchange.Redis;

namespace InventoryHold.Infrastructure.Redis;

public sealed class RedisRepository : IRedisRepository
{
    private readonly IDatabase database;

    public RedisRepository(IConnectionMultiplexer connection)
    {
        database = connection.GetDatabase();
    }

    public async Task<bool> AddKeyWithTtlAsync(
        string key,
        RedisValue value,
        TimeSpan ttl,
        CommandFlags flags = CommandFlags.None)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ttl, TimeSpan.Zero);

        if (await database.StringSetAsync(key, value, ttl, When.NotExists, flags))
        {
            return true;
        }

        if (!await database.StringSetAsync(key, value, ttl, When.Always, flags))
        {
            throw new InvalidOperationException($"The Redis key '{key}' could not be added or updated.");
        }

        return false;
    }

    public async Task<string> GetValueAsync(string key, CommandFlags flags = CommandFlags.None)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        RedisValue value = await database.StringGetAsync(key, flags);
        return value.HasValue
            ? value.ToString()
            : $"The Redis key '{key}' was not found.";
    }
}