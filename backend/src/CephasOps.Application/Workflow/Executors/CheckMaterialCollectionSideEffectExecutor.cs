using CephasOps.Application.Orders.Services;
using CephasOps.Application.Notifications.Services;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Domain.ServiceInstallers.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Workflow.Executors;

/// <summary>
/// Executor for checking material collection requirements when order is assigned to SI
/// Creates notifications for both SI and Admin if materials are missing
/// </summary>
public class CheckMaterialCollectionSideEffectExecutor : ISideEffectExecutor
{
    public string Key => "checkMaterialCollection";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly MaterialCollectionService _materialCollectionService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CheckMaterialCollectionSideEffectExecutor> _logger;

    public CheckMaterialCollectionSideEffectExecutor(
        ApplicationDbContext context,
        MaterialCollectionService materialCollectionService,
        INotificationService notificationService,
        ILogger<CheckMaterialCollectionSideEffectExecutor> logger)
    {
        _context = context;
        _materialCollectionService = materialCollectionService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        Guid entityId,
        WorkflowTransitionDto transition,
        Dictionary<string, object>? payload,
        Dictionary<string, object>? config,
        CancellationToken cancellationToken = default)
    {
        // Only execute when transitioning to "Assigned" status
        if (transition.ToStatus?.ToLowerInvariant() != "assigned")
        {
            _logger.LogDebug("Skipping material collection check - not transitioning to Assigned status");
            return;
        }

        _logger.LogInformation("Checking material collection for order {OrderId}", entityId);

        try
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == entityId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for material collection check", entityId);
                return;
            }

            if (!order.AssignedSiId.HasValue)
            {
                _logger.LogWarning("Order {OrderId} has no assigned SI, skipping material collection check", entityId);
                return;
            }

            // Check material collection requirements
            var checkResult = await _materialCollectionService.CheckMaterialCollectionAsync(
                entityId,
                order.CompanyId,
                cancellationToken);

            if (!checkResult.RequiresCollection)
            {
                _logger.LogInformation("All materials available for order {OrderId}", entityId);
                return;
            }

            _logger.LogWarning(
                "Order {OrderId} requires material collection. Missing {Count} materials",
                entityId,
                checkResult.MissingMaterials.Count);

            // Get SI user ID for notification
            ServiceInstaller? si = null;
            Guid? siUserId = null;
            if (order.AssignedSiId.HasValue)
            {
                si = await _context.ServiceInstallers
                    .FirstOrDefaultAsync(s => s.Id == order.AssignedSiId.Value, cancellationToken);
                if (si != null && si.UserId.HasValue)
                {
                    siUserId = si.UserId.Value;
                }
            }

            // Create notification metadata
            var metadata = new
            {
                orderId = entityId.ToString(),
                orderNumber = order.ServiceId ?? "N/A",
                serviceInstallerId = order.AssignedSiId?.ToString() ?? "N/A",
                serviceInstallerName = si?.Name ?? "Unknown",
                missingMaterials = checkResult.MissingMaterials.Select(m => new
                {
                    materialId = m.MaterialId.ToString(),
                    materialCode = m.MaterialCode,
                    materialName = m.MaterialName,
                    requiredQuantity = m.RequiredQuantity,
                    availableQuantity = m.AvailableQuantity,
                    missingQuantity = m.MissingQuantity,
                    unitOfMeasure = m.UnitOfMeasure
                }).ToList()
            };

            var metadataJson = JsonSerializer.Serialize(metadata);

            // Notify SI
            if (siUserId.HasValue)
            {
                var siMessage = $"Order {order.ServiceId ?? "N/A"} requires materials. " +
                               $"Please collect {checkResult.MissingMaterials.Count} material(s) from warehouse.";

                await _notificationService.CreateNotificationAsync(
                    new Application.Notifications.DTOs.CreateNotificationDto
                    {
                        UserId = siUserId.Value,
                        CompanyId = order.CompanyId,
                        Type = "MaterialCollectionRequired",
                        Priority = "High",
                        Title = "Materials Required for Order",
                        Message = siMessage,
                        ActionUrl = $"/jobs/{entityId}",
                        ActionText = "View Order",
                        RelatedEntityId = entityId,
                        RelatedEntityType = "Order",
                        MetadataJson = metadataJson
                    },
                    cancellationToken);

                _logger.LogInformation("Notification sent to SI {SiId} for material collection", siUserId.Value);
            }

            // Notify Admins (users with Admin or SuperAdmin role)
            var adminUsers = await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.Role != null &&
                            (ur.Role.Name == "Admin" || ur.Role.Name == "SuperAdmin"))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (adminUsers.Any())
            {
                var adminMessage = $"SI {si?.Name ?? "Unknown"} needs materials for Order {order.ServiceId ?? "N/A"}. " +
                                  $"Missing: {string.Join(", ", checkResult.MissingMaterials.Select(m => $"{m.MaterialName} ({m.MissingQuantity} {m.UnitOfMeasure})"))}";

                foreach (var adminUserId in adminUsers)
                {
                    await _notificationService.CreateNotificationAsync(
                        new Application.Notifications.DTOs.CreateNotificationDto
                        {
                            UserId = adminUserId,
                            CompanyId = order.CompanyId,
                            Type = "MaterialCollectionRequired",
                            Priority = "Normal",
                            Title = "SI Needs Materials",
                            Message = adminMessage,
                            ActionUrl = $"/orders/{entityId}",
                            ActionText = "View Order",
                            RelatedEntityId = entityId,
                            RelatedEntityType = "Order",
                            MetadataJson = metadataJson
                        },
                        cancellationToken);
                }

                _logger.LogInformation("Notifications sent to {Count} admin users for material collection", adminUsers.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking material collection for order {OrderId}", entityId);
            // Don't throw - this is a side effect, shouldn't block the transition
        }
    }
}

