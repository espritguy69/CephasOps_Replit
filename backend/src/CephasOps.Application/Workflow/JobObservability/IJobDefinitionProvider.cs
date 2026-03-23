namespace CephasOps.Application.Workflow.JobObservability;

/// <summary>
/// Provides job type metadata (display name, retry policy, stuck threshold) for observability and validation.
/// </summary>
public interface IJobDefinitionProvider
{
    /// <summary>
    /// Get definition for a job type. Returns null if unknown (caller can use fallbacks).
    /// </summary>
    Task<JobDefinitionDto?> GetByJobTypeAsync(string jobType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all registered job definitions (for admin UI or dropdowns).
    /// </summary>
    Task<IReadOnlyList<JobDefinitionDto>> GetAllAsync(CancellationToken cancellationToken = default);
}

public class JobDefinitionDto
{
    public Guid Id { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool RetryAllowed { get; set; }
    public int MaxRetries { get; set; }
    public int? DefaultStuckThresholdSeconds { get; set; }
}
