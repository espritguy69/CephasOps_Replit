using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.JobOrchestration;

/// <summary>
/// Resolves IJobExecutor by JobType from registered executors (Phase 3).
/// </summary>
public class JobExecutorRegistry : IJobExecutorRegistry
{
    private readonly IReadOnlyDictionary<string, IJobExecutor> _byType;
    private readonly ILogger<JobExecutorRegistry> _logger;

    public JobExecutorRegistry(IEnumerable<IJobExecutor> executors, ILogger<JobExecutorRegistry> logger)
    {
        _logger = logger;
        _byType = executors.ToDictionary(e => e.JobType.Trim().ToLowerInvariant(), e => e, StringComparer.OrdinalIgnoreCase);
        _logger.LogInformation("Job executor registry loaded {Count} types: {Types}", _byType.Count, string.Join(", ", _byType.Keys));
    }

    /// <inheritdoc />
    public IJobExecutor? GetExecutor(string jobType)
    {
        if (string.IsNullOrEmpty(jobType)) return null;
        return _byType.TryGetValue(jobType.Trim().ToLowerInvariant(), out var ex) ? ex : null;
    }
}
