using System.Collections.Concurrent;

namespace CephasOps.Api.Services.RateLimit;

/// <summary>In-memory rate limit store (per-process). Use when Redis is not configured.</summary>
public sealed class InMemoryRateLimitStore : IRateLimitStore
{
    private static readonly ConcurrentDictionary<Guid, Bucket> Buckets = new();

    public Task<(bool Allowed, string? LimitType)> TryConsumeAsync(Guid tenantId, int perMinute, int perHour, CancellationToken cancellationToken = default)
    {
        var bucket = Buckets.AddOrUpdate(
            tenantId,
            _ => new Bucket(perMinute, perHour),
            (_, b) => b.WithLimits(perMinute, perHour));
        var allowed = bucket.TryConsume(out var limitType);
        return Task.FromResult((allowed, limitType));
    }

    private sealed class Bucket
    {
        private int _perMinute;
        private int _perHour;
        private readonly Queue<DateTime> _minuteTimestamps = new();
        private readonly Queue<DateTime> _hourTimestamps = new();
        private readonly object _lock = new();

        public Bucket(int perMinute, int perHour)
        {
            _perMinute = perMinute;
            _perHour = perHour;
        }

        public Bucket WithLimits(int perMinute, int perHour)
        {
            _perMinute = perMinute;
            _perHour = perHour;
            return this;
        }

        public bool TryConsume(out string? limitType)
        {
            limitType = null;
            var now = DateTime.UtcNow;
            var minuteStart = now.AddMinutes(-1);
            var hourStart = now.AddHours(-1);
            lock (_lock)
            {
                while (_minuteTimestamps.Count > 0 && _minuteTimestamps.Peek() < minuteStart)
                    _minuteTimestamps.Dequeue();
                while (_hourTimestamps.Count > 0 && _hourTimestamps.Peek() < hourStart)
                    _hourTimestamps.Dequeue();
                if (_minuteTimestamps.Count >= _perMinute) { limitType = "Minute"; return false; }
                if (_hourTimestamps.Count >= _perHour) { limitType = "Hour"; return false; }
                _minuteTimestamps.Enqueue(now);
                _hourTimestamps.Enqueue(now);
                return true;
            }
        }
    }
}
