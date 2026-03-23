namespace CephasOps.Application.Workflow.JobOrchestration;

/// <summary>
/// Resolves IJobExecutor by job type (Phase 3).
/// </summary>
public interface IJobExecutorRegistry
{
    IJobExecutor? GetExecutor(string jobType);
}
