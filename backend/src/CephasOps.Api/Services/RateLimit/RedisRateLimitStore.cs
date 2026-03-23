using StackExchange.Redis;

namespace CephasOps.Api.Services.RateLimit;

/// <summary>Redis-backed rate limit store (fixed window per minute and per hour). Use when multiple API replicas need shared rate limit state.</summary>
public sealed class RedisRateLimitStore : IRateLimitStore
{
    private readonly IConnectionMultiplexer _redis;
    private const string KeyPrefix = "ratelimit:tenant:";

    public RedisRateLimitStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<(bool Allowed, string? LimitType)> TryConsumeAsync(Guid tenantId, int perMinute, int perHour, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var now = DateTime.UtcNow;
        var minuteSlot = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);
        var hourSlot = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        var id = tenantId.ToString("N");
        var keyMinute = $"{KeyPrefix}{id}:m:{minuteSlot:O}";
        var keyHour = $"{KeyPrefix}{id}:h:{hourSlot:O}";

        var script = @"
            local m = redis.call('INCR', KEYS[1])
            if m == 1 then redis.call('PEXPIRE', KEYS[1], 120000) end
            if m > tonumber(ARGV[1]) then return {0, 'Minute'} end
            local h = redis.call('INCR', KEYS[2])
            if h == 1 then redis.call('PEXPIRE', KEYS[2], 7200000) end
            if h > tonumber(ARGV[2]) then return {0, 'Hour'} end
            return {1, ''}
        ";
        var keys = new RedisKey[] { keyMinute, keyHour };
        var values = new RedisValue[] { perMinute, perHour };
        RedisResult raw;
        try
        {
            raw = await db.ScriptEvaluateAsync(script, keys, values);
        }
        catch
        {
            return (true, null); // fail open on Redis errors
        }

        if (raw.IsNull)
            return (true, null);
        var arr = (RedisValue[]?)raw;
        if (arr == null || arr.Length < 2)
            return (true, null);
        var allowed = arr[0].ToString() == "1";
        var limitType = arr[1].ToString();
        return (allowed, string.IsNullOrEmpty(limitType) ? null : limitType);
    }
}
