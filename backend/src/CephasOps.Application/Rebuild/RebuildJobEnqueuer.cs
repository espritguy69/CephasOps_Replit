using System.Text.Json;
using CephasOps.Application.Rebuild.DTOs;
using CephasOps.Application.Workflow.JobOrchestration;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Rebuild;

/// <summary>
/// Creates a Pending RebuildOperation and enqueues an OperationalRebuild job via JobExecution (Phase 11).
/// </summary>
public sealed class RebuildJobEnqueuer : IRebuildJobEnqueuer
{
    public const string JobType = "operationalrebuild";

    private readonly ApplicationDbContext _context;
    private readonly IJobExecutionEnqueuer _enqueuer;
    private readonly ILogger<RebuildJobEnqueuer> _logger;

    public RebuildJobEnqueuer(ApplicationDbContext context, IJobExecutionEnqueuer enqueuer, ILogger<RebuildJobEnqueuer> logger)
    {
        _context = context;
        _enqueuer = enqueuer;
        _logger = logger;
    }

    public async Task<Guid> EnqueueRebuildAsync(RebuildRequestDto request, Guid? scopeCompanyId, Guid? requestedByUserId, CancellationToken cancellationToken = default)
    {
        if (request.DryRun)
            throw new InvalidOperationException("Cannot enqueue a dry-run rebuild. Use sync execute with DryRun=true or preview API.");

        var utcNow = DateTime.UtcNow;
        var operation = new RebuildOperation
        {
            Id = Guid.NewGuid(),
            RebuildTargetId = request.RebuildTargetId,
            RequestedByUserId = requestedByUserId,
            RequestedAtUtc = utcNow,
            ScopeCompanyId = request.CompanyId ?? scopeCompanyId,
            FromOccurredAtUtc = request.FromOccurredAtUtc,
            ToOccurredAtUtc = request.ToOccurredAtUtc,
            DryRun = false,
            State = RebuildOperationStates.Pending,
            BackgroundJobId = null
        };

        var payload = new Dictionary<string, object?>
        {
            ["rebuildOperationId"] = operation.Id.ToString("N"),
            ["scopeCompanyId"] = scopeCompanyId?.ToString("N"),
            ["requestedByUserId"] = requestedByUserId?.ToString("N")
        };
        _context.RebuildOperations.Add(operation);
        await _context.SaveChangesAsync(cancellationToken);

        await _enqueuer.EnqueueAsync(
            JobType,
            JsonSerializer.Serialize(payload),
            companyId: scopeCompanyId ?? request.CompanyId,
            maxAttempts: 2,
            cancellationToken: cancellationToken);
        _logger.LogInformation("Enqueued operational rebuild job for RebuildOperation {OperationId} via JobExecution", operation.Id);
        return operation.Id;
    }

    public async Task EnqueueResumeAsync(Guid operationId, Guid? scopeCompanyId, Guid? requestedByUserId, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["rebuildOperationId"] = operationId.ToString("N"),
            ["scopeCompanyId"] = scopeCompanyId?.ToString("N"),
            ["requestedByUserId"] = requestedByUserId?.ToString("N")
        };
        await _enqueuer.EnqueueAsync(
            JobType,
            JsonSerializer.Serialize(payload),
            companyId: scopeCompanyId,
            maxAttempts: 2,
            cancellationToken: cancellationToken);
        _logger.LogInformation("Enqueued resume job for RebuildOperation {OperationId} via JobExecution", operationId);
    }
}
