using CephasOps.Application.Events.Replay;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Tasks.DTOs;
using CephasOps.Application.Tasks.Services;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Events;

/// <summary>
/// Event-driven operations when an order is assigned: create installer task, refresh material pack, enqueue SLA evaluation.
/// Task creation is idempotent by OrderId; material pack refresh is read-only. SLA job enqueue is skipped during replay to prevent duplicate jobs.
/// </summary>
public sealed class OrderAssignedOperationsHandler : IDomainEventHandler<OrderAssignedEvent>
{
    private readonly ApplicationDbContext _context;
    private readonly ITaskService _taskService;
    private readonly IMaterialPackProvider _materialPackProvider;
    private readonly IReplayExecutionContextAccessor? _replayContextAccessor;
    private readonly ILogger<OrderAssignedOperationsHandler> _logger;

    public OrderAssignedOperationsHandler(
        ApplicationDbContext context,
        ITaskService taskService,
        IMaterialPackProvider materialPackProvider,
        ILogger<OrderAssignedOperationsHandler> logger,
        IReplayExecutionContextAccessor? replayContextAccessor = null)
    {
        _context = context;
        _taskService = taskService;
        _materialPackProvider = materialPackProvider;
        _logger = logger;
        _replayContextAccessor = replayContextAccessor;
    }

    public async Task HandleAsync(OrderAssignedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var orderId = domainEvent.OrderId;
        var companyId = domainEvent.CompanyId ?? Guid.Empty;

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("OrderAssignedEvent: Order {OrderId} not found", orderId);
            return;
        }

        if (!order.AssignedSiId.HasValue)
        {
            _logger.LogDebug("OrderAssignedEvent: Order {OrderId} has no AssignedSiId, skipping task creation", orderId);
        }
        else
        {
            var si = await _context.ServiceInstallers
                .FirstOrDefaultAsync(s => s.Id == order.AssignedSiId.Value, cancellationToken);
            if (si != null && si.UserId.HasValue)
            {
                var title = string.IsNullOrEmpty(order.ServiceId)
                    ? $"Complete job: Order {orderId:N}"
                    : $"Complete job: Order {order.ServiceId}";
                var dto = new CreateTaskDto
                {
                    OrderId = orderId,
                    AssignedToUserId = si.UserId.Value,
                    Title = title,
                    Priority = "Normal"
                };
                await _taskService.CreateTaskAsync(dto, order.CompanyId ?? companyId, si.UserId.Value, cancellationToken);
                _logger.LogInformation("OrderAssignedEvent: installer task created for order {OrderId}", orderId);
            }
        }

        try
        {
            await _materialPackProvider.GetMaterialPackAsync(orderId, order.CompanyId ?? companyId, cancellationToken);
            _logger.LogDebug("OrderAssignedEvent: material pack refreshed for order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OrderAssignedEvent: material pack refresh failed for order {OrderId}", orderId);
        }

        var isReplay = _replayContextAccessor?.Current?.IsReplay ?? false;
        if (isReplay)
        {
            _logger.LogDebug(
                "OrderAssignedEvent: replay context active, skipping SLA job enqueue to prevent duplicate. EventId={EventId}, OrderId={OrderId}, Operation=OrderAssignedOperationsHandler, GuardReason=ReplaySkipSlaEnqueue",
                domainEvent.EventId, orderId);
            return;
        }

        var hasPendingSla = await _context.BackgroundJobs
            .AnyAsync(j => j.JobType == "slaevaluation"
                && (j.State == BackgroundJobState.Queued || j.State == BackgroundJobState.Running),
                cancellationToken);
        if (hasPendingSla)
        {
            _logger.LogDebug("OrderAssignedEvent: SLA evaluation already queued or running, skipping enqueue for order {OrderId}", orderId);
            return;
        }

        var jobCompanyId = order.CompanyId ?? companyId;
        if (jobCompanyId == Guid.Empty)
        {
            _logger.LogWarning("OrderAssignedEvent: Order {OrderId} has no CompanyId; skipping SLA job enqueue (tenant-boundary).", orderId);
            return;
        }

        var payload = new Dictionary<string, object> { ["companyId"] = jobCompanyId.ToString() };
        var now = DateTime.UtcNow;
        var job = new BackgroundJob
        {
            Id = Guid.NewGuid(),
            CompanyId = jobCompanyId,
            JobType = "slaevaluation",
            PayloadJson = JsonSerializer.Serialize(payload),
            State = BackgroundJobState.Queued,
            Priority = 0,
            ScheduledAt = now,
            MaxRetries = 2,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.BackgroundJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("OrderAssignedEvent: enqueued SLA evaluation job {JobId} for order {OrderId}", job.Id, orderId);
    }
}
