using CephasOps.Domain.Workflow.Entities;

namespace CephasOps.Application.Workflow.JobOrchestration;

/// <summary>
/// Executes a single job type (Phase 3). Registry resolves by JobType.
/// </summary>
public interface IJobExecutor
{
    string JobType { get; }
    Task<bool> ExecuteAsync(JobExecution job, CancellationToken cancellationToken = default);
}
