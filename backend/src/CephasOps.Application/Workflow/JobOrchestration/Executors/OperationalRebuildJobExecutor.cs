using CephasOps.Application.Rebuild;
using CephasOps.Application.Workers;
using CephasOps.Domain.Workflow.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Workflow.JobOrchestration.Executors;

/// <summary>
/// Executes an operational rebuild by operation id (Phase 11). Payload: rebuildOperationId, scopeCompanyId?, requestedByUserId?.
/// Uses worker claim so only one worker runs a given operation. Replaces legacy OperationalRebuild BackgroundJob execution.
/// </summary>
public sealed class OperationalRebuildJobExecutor : IJobExecutor
{
    public string JobType => "operationalrebuild";

    private readonly IWorkerIdentity _workerIdentity;
    private readonly IWorkerCoordinator _coordinator;
    private readonly IOperationalRebuildService _rebuildService;
    private readonly ILogger<OperationalRebuildJobExecutor> _logger;

    public OperationalRebuildJobExecutor(
        IWorkerIdentity workerIdentity,
        IWorkerCoordinator coordinator,
        IOperationalRebuildService rebuildService,
        ILogger<OperationalRebuildJobExecutor> logger)
    {
        _workerIdentity = workerIdentity;
        _coordinator = coordinator;
        _rebuildService = rebuildService;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(JobExecution job, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(job.PayloadJson))
            throw new ArgumentException("operationalrebuild job requires payload with rebuildOperationId");

        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(job.PayloadJson);
        if (payload == null || !payload.TryGetValue("rebuildOperationId", out var opEl))
            throw new ArgumentException("rebuildOperationId is required for OperationalRebuild job");
        var opStr = opEl.ValueKind == JsonValueKind.String ? opEl.GetString() : opEl.ToString();
        if (string.IsNullOrEmpty(opStr) || (!Guid.TryParse(opStr!.Replace("-", ""), out var operationId) && !Guid.TryParse(opStr, out operationId)))
            throw new ArgumentException("Invalid rebuildOperationId in OperationalRebuild payload");

        Guid? scopeCompanyId = TryGetGuid(payload, "scopeCompanyId");
        Guid? requestedByUserId = TryGetGuid(payload, "requestedByUserId");

        var workerId = _workerIdentity.WorkerId;
        if (workerId.HasValue)
        {
            var claimed = await _coordinator.TryClaimRebuildOperationAsync(workerId.Value, operationId, cancellationToken).ConfigureAwait(false);
            if (!claimed)
            {
                _logger.LogInformation("Rebuild operation {OpId} already claimed by another worker; skipping job {JobId}", operationId, job.Id);
                return true;
            }
        }

        try
        {
            var result = await _rebuildService.ExecuteByOperationIdAsync(operationId, scopeCompanyId, requestedByUserId, cancellationToken);
            if (!string.IsNullOrEmpty(result.ErrorMessage) && result.State == RebuildOperationStates.Failed)
                throw new InvalidOperationException($"Operational rebuild failed: {result.ErrorMessage}");
            _logger.LogInformation(
                "Operational rebuild job completed. RebuildOperationId={OpId}, State={State}, Inserted={Inserted}",
                operationId, result.State, result.RowsInserted);
            return true;
        }
        finally
        {
            if (workerId.HasValue)
            {
                try
                {
                    await _coordinator.ReleaseRebuildOperationAsync(operationId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to release rebuild operation ownership. RebuildOperationId={OpId}", operationId);
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
