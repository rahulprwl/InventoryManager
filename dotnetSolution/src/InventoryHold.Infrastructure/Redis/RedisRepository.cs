using StackExchange.Redis;

namespace InventoryHold.Infrastructure.Redis;

public sealed class RedisRepository : IRedisRepository
{
    private readonly IDatabase database;
    private const string ReleaseLockScript = "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

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

    public Task SetValueAsync(string key, RedisValue value, TimeSpan ttl, CommandFlags flags = CommandFlags.None)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ttl, TimeSpan.Zero);
        return database.StringSetAsync(key, value, ttl, When.Always, flags);
    }

    public async Task<string?> GetNullableValueAsync(string key, CommandFlags flags = CommandFlags.None)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var value = await database.StringGetAsync(key, flags);
        return value.HasValue ? value.ToString() : null;
    }

    public Task<bool> AcquireLockAsync(string key, string token, TimeSpan ttl, CommandFlags flags = CommandFlags.None)
    {
        return database.StringSetAsync(key, token, ttl, When.NotExists, flags);
    }

    public async Task<bool> ReleaseLockAsync(string key, string token, CommandFlags flags = CommandFlags.None)
    {
        var result = await database.ScriptEvaluateAsync(
            ReleaseLockScript,
            new RedisKey[] { key },
            new RedisValue[] { token },
            flags);
        return (int)result == 1;
    }
}