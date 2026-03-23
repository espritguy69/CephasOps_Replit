namespace CephasOps.Domain.Workflow.Entities;

/// <summary>
/// Metadata for a job type: display name, retry policy, and stuck threshold.
/// Used for retry validation, stuck detection, and UI display.
/// </summary>
public class JobDefinition
{
    /// <summary>Unique identifier (can match JobType string or be a separate id).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Job type key (e.g. EmailIngest, pnlrebuild).</summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>Display name for UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Whether manual retry from UI/API is allowed.</summary>
    public bool RetryAllowed { get; set; } = true;

    /// <summary>Default max retries when enqueueing (can be overridden per job).</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Default stuck threshold in seconds (running longer than this is considered stuck). Null = use global default.</summary>
    public int? DefaultStuckThresholdSeconds { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
