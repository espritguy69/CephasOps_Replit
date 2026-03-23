using CephasOps.Application.Events.Replay;
using CephasOps.Application.Workers;
using CephasOps.Domain.Workflow.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Workflow.JobOrchestration.Executors;

/// <summary>
/// Executes an operational replay by operation id (Phase 11). Payload: replayOperationId, scopeCompanyId?, requestedByUserId?.
/// Uses worker claim so only one worker runs a given operation. Replaces legacy OperationalReplay BackgroundJob execution.
/// </summary>
public sealed class OperationalReplayJobExecutor : IJobExecutor
{
    public string JobType => "operationalreplay";

    private readonly IWorkerIdentity _workerIdentity;
    private readonly IWorkerCoordinator _coordinator;
    private readonly IOperationalReplayExecutionService _executionService;
    private readonly ILogger<OperationalReplayJobExecutor> _logger;

    public OperationalReplayJobExecutor(
        IWorkerIdentity workerIdentity,
        IWorkerCoordinator coordinator,
        IOperationalReplayExecutionService executionService,
        ILogger<OperationalReplayJobExecutor> logger)
    {
        _workerIdentity = workerIdentity;
        _coordinator = coordinator;
        _executionService = executionService;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(JobExecution job, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(job.PayloadJson))
            throw new ArgumentException("operationalreplay job requires payload with replayOperationId");

        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(job.PayloadJson);
        if (payload == null || !payload.TryGetValue("replayOperationId", out var opEl))
            throw new ArgumentException("replayOperationId is required for OperationalReplay job");
        var opStr = opEl.ValueKind == JsonValueKind.String ? opEl.GetString() : opEl.ToString();
        if (string.IsNullOrEmpty(opStr) || (!Guid.TryParse(opStr!.Replace("-", ""), out var operationId) && !Guid.TryParse(opStr, out operationId)))
            throw new ArgumentException("Invalid replayOperationId in OperationalReplay payload");

        Guid? scopeCompanyId = TryGetGuid(payload, "scopeCompanyId");
        Guid? requestedByUserId = TryGetGuid(payload, "requestedByUserId");

        var workerId = _workerIdentity.WorkerId;
        if (workerId.HasValue)
        {
            var claimed = await _coordinator.TryClaimReplayOperationAsync(workerId.Value, operationId, cancellationToken).ConfigureAwait(false);
            if (!claimed)
            {
                _logger.LogInformation("Replay operation {OpId} already claimed by another worker; skipping job {JobId}", operationId, job.Id);
                return true;
            }
        }

        try
        {
            var result = await _executionService.ExecuteByOperationIdAsync(operationId, scopeCompanyId, requestedByUserId, cancellationToken);
            if (!string.IsNullOrEmpty(result.ErrorMessage))
                throw new InvalidOperationException($"Operational replay failed: {result.ErrorMessage}");
            _logger.LogInformation(
                "Operational replay job completed. ReplayOperationId={OpId}, State={State}, Executed={Executed}, Succeeded={Ok}, Failed={Fail}",
                operationId, result.State, result.TotalExecuted, result.TotalSucceeded, result.TotalFailed);
            return true;
        }
        finally
        {
            if (workerId.HasValue)
            {
                try
                {
                    await _coordinator.ReleaseReplayOperationAsync(operationId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to release replay operation ownership. ReplayOperationId={OpId}", operationId);
                }
            }
        }
    }

    private static Guid? TryGetGuid(Dictionary<string, JsonElement> payload, string key)
    {
        if (!payload.TryGetValue(key, out var el)) return null;
        var s = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        if (string.IsNullOrEmpty(s)) return null;
        if (Guid.TryParse(s.Replace("-", ""), out var g) || Guid.TryParse(s, out g)) return g;
        return null;
    }
}
