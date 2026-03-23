using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// Defines available side effects that can be executed in workflow transitions
/// Stored in settings - fully configurable, no hardcoding
/// </summary>
public class SideEffectDefinition : BaseEntity
{
    /// <summary>
    /// Company ID this side effect belongs to
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Unique identifier for the side effect (e.g., "notify", "createStockMovement")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Display name (e.g., "Send Notification")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this side effect does
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Entity type this side effect applies to (e.g., "Order", "Invoice")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Executor type name (e.g., "NotificationSideEffectExecutor", "StockMovementSideEffectExecutor")
    /// This maps to an executor class that implements ISideEffectExecutor
    /// </summary>
    public string ExecutorType { get; set; } = string.Empty;

    /// <summary>
    /// JSON configuration for the executor (e.g., {"template": "OrderStatusChange", "recipients": ["SI", "Admin"]})
    /// </summary>
    public string? ExecutorConfigJson { get; set; }

    /// <summary>
    /// Whether this side effect is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order in UI
    /// </summary>
    public int DisplayOrder { get; set; } = 0;
}

