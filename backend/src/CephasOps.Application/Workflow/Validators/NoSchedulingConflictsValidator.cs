using CephasOps.Application.Scheduler.Services;
using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace CephasOps.Application.Workflow.Validators;

/// <summary>
/// Validator for checking if there are no unresolved scheduling conflicts for an order
/// Configurable via GuardConditionDefinition.ValidatorConfigJson
/// Blocks transition to "InProgress" if conflicts exist
/// </summary>
public class NoSchedulingConflictsValidator : IGuardConditionValidator
{
    public string Key => "noSchedulingConflicts";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NoSchedulingConflictsValidator> _logger;

    public NoSchedulingConflictsValidator(
        ApplicationDbContext context,
        IServiceProvider serviceProvider,
        ILogger<NoSchedulingConflictsValidator> logger)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(
        Guid entityId,
        Dictionary<string, object>? config,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == entityId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for scheduling conflict validation", entityId);
            return false;
        }

        // Get company ID from order
        var companyId = order.CompanyId;

        // Find the scheduled slot for this order
        var slot = await _context.ScheduledSlots
            .Where(s => s.OrderId == entityId && s.Status != "Cancelled")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (slot == null)
        {
            // No slot found - no conflicts possible
            _logger.LogDebug("Order {OrderId} has no scheduled slot, no conflicts to check", entityId);
            return true;
        }

        if (!order.DepartmentId.HasValue)
        {
            // Order has no department; cannot scope conflict detection. Treat as no conflicts (never call with departmentId: null).
            _logger.LogDebug("Order {OrderId} has no department; skipping department-scoped conflict check", entityId);
            return true;
        }

        // Check for conflicts using the scheduler service (lazily resolved to break circular dependency).
        // Never call with departmentId: null; order.DepartmentId is validated above.
        var schedulerService = _serviceProvider.GetRequiredService<ISchedulerService>();
        var conflicts = await schedulerService.DetectSchedulingConflictsAsync(
            orderId: entityId,
            slotId: slot.Id,
            siId: slot.ServiceInstallerId,
            date: slot.Date,
            companyId: companyId,
            departmentId: order.DepartmentId,
            cancellationToken);

        if (conflicts.Count > 0)
        {
            // Build detailed error message with conflict information
            var conflictDetails = string.Join("\n", conflicts.Select((c, idx) => 
                $"{idx + 1}. {c.ConflictDescription} - Order: {c.OrderServiceId} ({c.OrderCustomerName}) at {c.WindowFrom:hh\\:mm}-{c.WindowTo:hh\\:mm}"));

            _logger.LogWarning(
                "Order {OrderId} has {ConflictCount} unresolved scheduling conflict(s). Details: {ConflictDetails}",
                entityId,
                conflicts.Count,
                conflictDetails);

            // Store conflict details in config for error message (if config is provided)
            if (config != null)
            {
                config["conflictCount"] = conflicts.Count;
                config["conflicts"] = conflicts.Select(c => new Dictionary<string, object>
                {
                    ["slotId"] = c.SlotId.ToString(),
                    ["orderId"] = c.OrderId.ToString(),
                    ["orderServiceId"] = c.OrderServiceId ?? "",
                    ["orderCustomerName"] = c.OrderCustomerName ?? "",
                    ["conflictType"] = c.ConflictType,
                    ["conflictDescription"] = c.ConflictDescription,
                    ["windowFrom"] = c.WindowFrom.ToString(@"hh\:mm"),
                    ["windowTo"] = c.WindowTo.ToString(@"hh\:mm")
                }).ToList();
            }

            return false; // Conflicts exist - validation fails
        }

        _logger.LogDebug("Order {OrderId} has no scheduling conflicts", entityId);
        return true; // No conflicts - validation passes
    }
}

