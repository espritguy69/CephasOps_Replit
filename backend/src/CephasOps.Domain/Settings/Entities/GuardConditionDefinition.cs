using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// Defines available guard conditions that can be used in workflow transitions
/// Stored in settings - fully configurable, no hardcoding
/// </summary>
public class GuardConditionDefinition : BaseEntity
{
    /// <summary>
    /// Company ID this guard condition belongs to
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Unique identifier for the guard condition (e.g., "photosRequired", "docketUploaded")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Display name (e.g., "Photos Required")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this guard condition checks
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Entity type this guard condition applies to (e.g., "Order", "Invoice")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Validator type name (e.g., "PhotosRequiredValidator", "DocketUploadedValidator")
    /// This maps to a validator class that implements IGuardConditionValidator
    /// </summary>
    public string ValidatorType { get; set; } = string.Empty;

    /// <summary>
    /// JSON configuration for the validator (e.g., {"checkFiles": true, "checkFlag": "PhotosUploaded"})
    /// </summary>
    public string? ValidatorConfigJson { get; set; }

    /// <summary>
    /// Whether this guard condition is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order in UI
    /// </summary>
    public int DisplayOrder { get; set; } = 0;
}

