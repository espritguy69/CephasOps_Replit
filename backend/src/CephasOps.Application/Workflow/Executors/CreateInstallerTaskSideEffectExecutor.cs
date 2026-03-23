using CephasOps.Application.Tasks.DTOs;
using CephasOps.Application.Tasks.Services;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Domain.ServiceInstallers.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Executors;

/// <summary>
/// Creates a single task for the assigned service installer when an order transitions to Assigned.
/// Idempotent: one task per order (via TaskService CreateTaskAsync with OrderId).
/// </summary>
public class CreateInstallerTaskSideEffectExecutor : ISideEffectExecutor
{
    public string Key => "createInstallerTask";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ITaskService _taskService;
    private readonly ILogger<CreateInstallerTaskSideEffectExecutor> _logger;

    public CreateInstallerTaskSideEffectExecutor(
        ApplicationDbContext context,
        ITaskService taskService,
        ILogger<CreateInstallerTaskSideEffectExecutor> logger)
    {
        _context = context;
        _taskService = taskService;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        Guid entityId,
        WorkflowTransitionDto transition,
        Dictionary<string, object>? payload,
        Dictionary<string, object>? config,
        CancellationToken cancellationToken = default)
    {
        if (transition.ToStatus?.ToLowerInvariant() != "assigned")
        {
            _logger.LogDebug("Skipping installer task creation - not transitioning to Assigned status");
            return;
        }

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == entityId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for installer task creation", entityId);
            return;
        }
        if (!order.AssignedSiId.HasValue)
        {
            _logger.LogWarning("Order {OrderId} has no assigned SI, skipping installer task creation", entityId);
            return;
        }

        ServiceInstaller? si = await _context.ServiceInstallers
            .FirstOrDefaultAsync(s => s.Id == order.AssignedSiId.Value, cancellationToken);
        if (si == null || !si.UserId.HasValue)
        {
            _logger.LogWarning("SI {SiId} not found or has no UserId for order {OrderId}", order.AssignedSiId.Value, entityId);
            return;
        }

        var title = string.IsNullOrEmpty(order.ServiceId)
            ? $"Complete job: Order {entityId:N}"
            : $"Complete job: Order {order.ServiceId}";

        var dto = new CreateTaskDto
        {
            OrderId = entityId,
            AssignedToUserId = si.UserId.Value,
            Title = title,
            Description = null,
            DueAt = null,
            Priority = "Normal"
        };

        await _taskService.CreateTaskAsync(dto, order.CompanyId ?? Guid.Empty, si.UserId.Value, cancellationToken);
        _logger.LogInformation("Installer task created for order {OrderId}, SI {SiId}", entityId, order.AssignedSiId.Value);
    }
}
