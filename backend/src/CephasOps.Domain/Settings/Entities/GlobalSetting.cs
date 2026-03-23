namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// Global setting entity - system-wide or default configuration values
/// </summary>
public class GlobalSetting
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Setting key (unique, e.g. "SnapshotRetentionDays")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Setting value (stored as string, may contain JSON)
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Value type (String, Int, Decimal, Bool, Json)
    /// </summary>
    public string ValueType { get; set; } = "String";

    /// <summary>
    /// Description of what this setting controls
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Module category (Parser, Email, PnL, Inventory, Security, UI, etc.)
    /// </summary>
    public string? Module { get; set; }

    /// <summary>
    /// Timestamp when the setting was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who created this setting
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the setting was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who last updated this setting
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }
}

