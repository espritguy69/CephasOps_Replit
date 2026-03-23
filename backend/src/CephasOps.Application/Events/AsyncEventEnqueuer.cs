using System.Text.Json;
using CephasOps.Application.Workflow.JobOrchestration;
using CephasOps.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Events;

/// <summary>
/// Enqueues async event handling via JobExecution (Phase 11). Payload contains eventId; executor loads event from store.
/// </summary>
public class AsyncEventEnqueuer : IAsyncEventEnqueuer
{
    public const string JobType = "eventhandlingasync";

    private readonly IJobExecutionEnqueuer _enqueuer;
    private readonly ILogger<AsyncEventEnqueuer> _logger;

    public AsyncEventEnqueuer(IJobExecutionEnqueuer enqueuer, ILogger<AsyncEventEnqueuer> logger)
    {
        _enqueuer = enqueuer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task EnqueueAsync(Guid eventId, IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["eventId"] = eventId.ToString("N"),
            ["correlationId"] = domainEvent.CorrelationId,
            ["companyId"] = domainEvent.CompanyId?.ToString("N")
        };
        await _enqueuer.EnqueueAsync(
            JobType,
            JsonSerializer.Serialize(payload),
            companyId: domainEvent.CompanyId,
            correlationId: domainEvent.CorrelationId,
            maxAttempts: 5,
            cancellationToken: cancellationToken);
        _logger.LogDebug("Enqueued async event handling job for event {EventId} via JobExecution", eventId);
    }
}
