using System.Text.Json;
using CephasOps.Application.Events.DTOs;
using CephasOps.Application.Workflow.JobOrchestration;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Creates a Pending ReplayOperation and enqueues an OperationalReplay job via JobExecution (Phase 11).
/// </summary>
public class ReplayJobEnqueuer : IReplayJobEnqueuer
{
    public const string JobType = "operationalreplay";

    private readonly ApplicationDbContext _context;
    private readonly IJobExecutionEnqueuer _enqueuer;
    private readonly ILogger<ReplayJobEnqueuer> _logger;

    public ReplayJobEnqueuer(ApplicationDbContext context, IJobExecutionEnqueuer enqueuer, ILogger<ReplayJobEnqueuer> logger)
    {
        _context = context;
        _enqueuer = enqueuer;
        _logger = logger;
    }

    public async Task<Guid> EnqueueReplayAsync(ReplayRequestDto request, Guid? scopeCompanyId, Guid? requestedByUserId, CancellationToken cancellationToken = default)
    {
        if (request.DryRun)
            throw new InvalidOperationException("Cannot enqueue a dry-run replay. Use preview API for dry-run.");

        var utcNow = DateTime.UtcNow;
        var operation = new ReplayOperation
        {
            Id = Guid.NewGuid(),
            RequestedByUserId = requestedByUserId,
            RequestedAtUtc = utcNow,
            DryRun = false,
            ReplayReason = request.ReplayReason,
            CompanyId = request.CompanyId,
            EventType = request.EventType,
            Status = request.Status,
            FromOccurredAtUtc = request.FromOccurredAtUtc,
            ToOccurredAtUtc = request.ToOccurredAtUtc,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            CorrelationId = request.CorrelationId,
            MaxEvents = request.MaxEvents,
            State = ReplayOperationStates.Pending,
            ReplayTarget = request.ReplayTarget ?? ReplayTargets.EventStore,
            ReplayMode = request.ReplayMode ?? ReplayModes.Apply,
            BackgroundJobId = null
        };

        var payload = new Dictionary<string, object?>
        {
            ["replayOperationId"] = operation.Id.ToString("N"),
            ["scopeCompanyId"] = scopeCompanyId?.ToString("N"),
            ["requestedByUserId"] = requestedByUserId?.ToString("N")
        };
        _context.ReplayOperations.Add(operation);
        await _context.SaveChangesAsync(cancellationToken);

        await _enqueuer.EnqueueAsync(
            JobType,
            JsonSerializer.Serialize(payload),
            companyId: scopeCompanyId ?? request.CompanyId,
            maxAttempts: 2,
            cancellationToken: cancellationToken);
        _logger.LogInformation("Enqueued operational replay job for ReplayOperation {OperationId} via JobExecution", operation.Id);
        return operation.Id;
    }

    public async Task EnqueueResumeAsync(Guid operationId, Guid? scopeCompanyId, Guid? requestedByUserId, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["replayOperationId"] = operationId.ToString("N"),
            ["scopeCompanyId"] = scopeCompanyId?.ToString("N"),
            ["requestedByUserId"] = requestedByUserId?.ToString("N")
        };
        await _enqueuer.EnqueueAsync(
            JobType,
            JsonSerializer.Serialize(payload),
            companyId: scopeCompanyId,
            maxAttempts: 2,
            cancellationToken: cancellationToken);
        _logger.LogInformation("Enqueued resume job for ReplayOperation {OperationId} via JobExecution", operationId);
    }
}
