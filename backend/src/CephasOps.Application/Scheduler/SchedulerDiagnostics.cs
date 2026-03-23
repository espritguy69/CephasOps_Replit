using System.Collections.Concurrent;

namespace CephasOps.Application.Scheduler;

/// <summary>
/// In-memory diagnostics for the job polling coordinator. Updated by JobPollingCoordinatorService; read by scheduler API.
/// </summary>
public sealed class SchedulerDiagnostics
{
    private const int RecentMax = 50;

    private DateTime? _lastPollUtc;
    private Guid? _workerId;
    private int _totalDiscovered;
    private int _totalClaimAttempts;
    private int _totalClaimSuccess;
    private int _totalClaimFailure;
    private readonly ConcurrentQueue<Guid> _recentDiscovered = new();
    private readonly ConcurrentQueue<(Guid JobId, bool Success)> _recentClaimAttempts = new();

    public DateTime? LastPollUtc => _lastPollUtc;
    public Guid? WorkerId => _workerId;

    public int TotalDiscovered => _totalDiscovered;
    public int TotalClaimAttempts => _totalClaimAttempts;
    public int TotalClaimSuccess => _totalClaimSuccess;
    public int TotalClaimFailure => _totalClaimFailure;

    public void SetWorkerId(Guid? workerId) => _workerId = workerId;

    public void RecordPoll(DateTime utc, int discoveredCount, IReadOnlyList<(Guid JobId, bool Claimed)> claimResults)
    {
        _lastPollUtc = utc;
        _totalDiscovered += discoveredCount;
        foreach (var (jobId, claimed) in claimResults)
        {
            _totalClaimAttempts++;
            if (claimed) _totalClaimSuccess++; else _totalClaimFailure++;
            Enqueue(_recentClaimAttempts, (jobId, claimed), RecentMax);
        }
        foreach (var (jobId, _) in claimResults)
            Enqueue(_recentDiscovered, jobId, RecentMax);
    }

    public IReadOnlyList<Guid> GetRecentDiscovered()
    {
        return _recentDiscovered.ToArray();
    }

    public IReadOnlyList<(Guid JobId, bool Success)> GetRecentClaimAttempts()
    {
        return _recentClaimAttempts.ToArray();
    }

    public (int Discovered, int Attempts, int Success, int Failure) GetTotals()
    {
        return (_totalDiscovered, _totalClaimAttempts, _totalClaimSuccess, _totalClaimFailure);
    }

    private static void Enqueue<T>(ConcurrentQueue<T> queue, T item, int max)
    {
        queue.Enqueue(item);
        while (queue.Count > max && queue.TryDequeue(out _)) { }
    }
}
