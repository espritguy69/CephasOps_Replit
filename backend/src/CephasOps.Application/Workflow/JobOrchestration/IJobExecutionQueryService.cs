namespace CephasOps.Application.Workflow.JobOrchestration;

/// <summary>Operational visibility over JobExecution (Phase 4).</summary>
public interface IJobExecutionQueryService
{
    Task<JobExecutionSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobExecutionListItemDto>> GetPendingAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobExecutionListItemDto>> GetRunningAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobExecutionListItemDto>> GetFailedRetryScheduledAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobExecutionListItemDto>> GetDeadLetterAsync(int limit = 100, CancellationToken cancellationToken = default);
}
