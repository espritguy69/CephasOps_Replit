using System.Diagnostics.Metrics;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Replay engine metrics for observability. Uses System.Diagnostics.Metrics (built-in); export via OpenTelemetry or similar if needed.
/// </summary>
public sealed class ReplayMetrics
{
    public const string MeterName = "CephasOps.Replay";

    private readonly Meter _meter;
    private readonly Counter<long> _runsStarted;
    private readonly Counter<long> _runsCompleted;
    private readonly Counter<long> _runsFailed;
    private readonly Counter<long> _runsResumed;
    private readonly Counter<long> _eventsProcessed;
    private readonly Counter<long> _eventsFailed;
    private readonly Histogram<double> _runDurationSeconds;
    private readonly Counter<long> _checkpointsWritten;
    private readonly Counter<long> _previewRequests;
    private readonly Counter<long> _reruns;

    public ReplayMetrics()
    {
        _meter = new Meter(MeterName, "1.0");
        _runsStarted = _meter.CreateCounter<long>("replay.runs.started", description: "Replay runs started");
        _runsCompleted = _meter.CreateCounter<long>("replay.runs.completed", description: "Replay runs completed successfully");
        _runsFailed = _meter.CreateCounter<long>("replay.runs.failed", description: "Replay runs failed");
        _runsResumed = _meter.CreateCounter<long>("replay.runs.resumed", description: "Replay runs resumed from checkpoint");
        _eventsProcessed = _meter.CreateCounter<long>("replay.events.processed", description: "Replay events processed");
        _eventsFailed = _meter.CreateCounter<long>("replay.events.failed", description: "Replay events failed");
        _runDurationSeconds = _meter.CreateHistogram<double>("replay.run.duration_seconds", unit: "s", description: "Replay run duration in seconds");
        _checkpointsWritten = _meter.CreateCounter<long>("replay.checkpoints.written", description: "Replay checkpoints written");
        _previewRequests = _meter.CreateCounter<long>("replay.preview.requests", description: "Replay preview requests");
        _reruns = _meter.CreateCounter<long>("replay.runs.rerun", description: "Replay reruns");
    }

    public void RecordRunStarted(string? replayTarget = null, bool dryRun = false)
    {
        var tags = new List<KeyValuePair<string, object?>>();
        if (!string.IsNullOrEmpty(replayTarget)) tags.Add(new KeyValuePair<string, object?>("replay_target", replayTarget));
        tags.Add(new KeyValuePair<string, object?>("dry_run", dryRun));
        _runsStarted.Add(1, tags.ToArray());
    }

    public void RecordRunCompleted(string? replayTarget, bool dryRun, int totalExecuted, int totalSucceeded, int totalFailed, double durationSeconds)
    {
        var tags = new List<KeyValuePair<string, object?>>();
        if (!string.IsNullOrEmpty(replayTarget)) tags.Add(new KeyValuePair<string, object?>("replay_target", replayTarget));
        tags.Add(new KeyValuePair<string, object?>("dry_run", dryRun));
        _runsCompleted.Add(1, tags.ToArray());
        _eventsProcessed.Add(totalExecuted, tags.ToArray());
        if (totalFailed > 0)
            _eventsFailed.Add(totalFailed, tags.ToArray());
        _runDurationSeconds.Record(durationSeconds, tags.ToArray());
    }

    public void RecordRunFailed(string? replayTarget, bool dryRun)
    {
        var tags = new List<KeyValuePair<string, object?>>();
        if (!string.IsNullOrEmpty(replayTarget)) tags.Add(new KeyValuePair<string, object?>("replay_target", replayTarget));
        tags.Add(new KeyValuePair<string, object?>("dry_run", dryRun));
        _runsFailed.Add(1, tags.ToArray());
    }

    public void RecordRunResumed(string? replayTarget)
    {
        var tags = new List<KeyValuePair<string, object?>>();
        if (!string.IsNullOrEmpty(replayTarget)) tags.Add(new KeyValuePair<string, object?>("replay_target", replayTarget));
        _runsResumed.Add(1, tags.ToArray());
    }

    public void RecordCheckpoint(string? replayTarget)
    {
        var tags = new List<KeyValuePair<string, object?>>();
        if (!string.IsNullOrEmpty(replayTarget)) tags.Add(new KeyValuePair<string, object?>("replay_target", replayTarget));
        _checkpointsWritten.Add(1, tags.ToArray());
    }

    public void RecordPreviewRequest(string? replayTarget)
    {
        var tags = new List<KeyValuePair<string, object?>>();
        if (!string.IsNullOrEmpty(replayTarget)) tags.Add(new KeyValuePair<string, object?>("replay_target", replayTarget));
        _previewRequests.Add(1, tags.ToArray());
    }

    public void RecordRerun(string? replayTarget, string rerunType)
    {
        var tags = new List<KeyValuePair<string, object?>>();
        if (!string.IsNullOrEmpty(replayTarget)) tags.Add(new KeyValuePair<string, object?>("replay_target", replayTarget));
        tags.Add(new KeyValuePair<string, object?>("rerun_type", rerunType));
        _reruns.Add(1, tags.ToArray());
    }
}
