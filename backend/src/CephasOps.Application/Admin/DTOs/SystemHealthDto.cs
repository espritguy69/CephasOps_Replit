namespace CephasOps.Application.Admin.DTOs;

/// <summary>
/// System health check response
/// </summary>
public class SystemHealthDto
{
    public bool IsHealthy { get; set; }
    public DateTime CheckedAt { get; set; }
    public string Version { get; set; } = string.Empty;
    public DatabaseHealthDto Database { get; set; } = new();
    /// <summary>
    /// Email parser / ingestion health (scheduler, active accounts, last poll times).
    /// </summary>
    public EmailParserHealthDto? EmailParser { get; set; }

    /// <summary>
    /// Background job runner status (counts, last activity).
    /// </summary>
    public BackgroundJobRunnerHealthDto? BackgroundJobRunner { get; set; }

    public Dictionary<string, string> Details { get; set; } = new();
}

/// <summary>
/// Background job runner health for admin/health.
/// </summary>
public class BackgroundJobRunnerHealthDto
{
    public string Status { get; set; } = "Unknown";
    public int RunningCount { get; set; }
    public int QueuedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime? LastActivityAt { get; set; }
}

/// <summary>
/// Email parser (ingestion) health status for monitoring and probes.
/// </summary>
public class EmailParserHealthDto
{
    /// <summary>
    /// Overall parser status: Healthy (polling within threshold), Degraded (stale or errors), NoAccounts (no active mailboxes).
    /// </summary>
    public string Status { get; set; } = "Unknown";

    /// <summary>
    /// Number of active (non-deleted) email accounts configured for polling.
    /// </summary>
    public int ActiveAccountsCount { get; set; }

    /// <summary>
    /// UTC time of the most recent successful poll across all accounts, or null if none have polled yet.
    /// </summary>
    public DateTime? MostRecentPollAt { get; set; }

    /// <summary>
    /// Number of active accounts that have not been polled within the expected window (e.g. 2× poll interval or 15 min).
    /// </summary>
    public int StaleAccountsCount { get; set; }

    /// <summary>
    /// Per-account summary for diagnostics (id, name, last poll, status).
    /// </summary>
    public List<EmailParserAccountHealthDto> Accounts { get; set; } = new();
}

/// <summary>
/// Per-account email parser health for one mailbox.
/// </summary>
public class EmailParserAccountHealthDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? LastPolledAt { get; set; }
    /// <summary>
    /// Minutes since last successful poll; null if never polled.
    /// </summary>
    public double? MinutesSinceLastPoll { get; set; }
    /// <summary>
    /// Healthy | Stale | NeverPolled
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Database health status
/// </summary>
public class DatabaseHealthDto
{
    public bool IsConnected { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan? ResponseTime { get; set; }
}

