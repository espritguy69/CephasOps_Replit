using System.Collections.Concurrent;
using CephasOps.Domain.PlatformSafety;

namespace CephasOps.Infrastructure.PlatformSafety;

/// <summary>
/// In-memory bounded buffer of recent platform guard violations for operational observability.
/// Thread-safe; drops oldest when full. Process restart clears the buffer.
/// </summary>
public sealed class GuardViolationBuffer : IGuardViolationBuffer
{
    private const int MaxEntries = 200;
    private const int MaxMessageLength = 500;

    private readonly ConcurrentQueue<GuardViolationEntry> _queue = new();

    /// <inheritdoc />
    public void Record(GuardViolationEntry entry)
    {
        if (entry == null) return;
        var copy = new GuardViolationEntry
        {
            OccurredAtUtc = entry.OccurredAtUtc,
            GuardName = entry.GuardName ?? "",
            Operation = entry.Operation ?? "",
            Message = Truncate(entry.Message ?? "", MaxMessageLength),
            CompanyId = entry.CompanyId,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            EventId = entry.EventId
        };
        _queue.Enqueue(copy);
        while (_queue.Count > MaxEntries && _queue.TryDequeue(out _)) { }
    }

    /// <inheritdoc />
    public IReadOnlyList<GuardViolationEntry> GetRecent(int maxCount)
    {
        var list = _queue.ToArray();
        if (list.Length == 0) return Array.Empty<GuardViolationEntry>();
        var take = Math.Min(maxCount, list.Length);
        var result = new GuardViolationEntry[take];
        for (var i = 0; i < take; i++)
            result[i] = list[list.Length - 1 - i];
        return result;
    }

    private static string Truncate(string value, int maxLength) =>
        string.IsNullOrEmpty(value) ? "" : value.Length <= maxLength ? value : value[..maxLength];
}
