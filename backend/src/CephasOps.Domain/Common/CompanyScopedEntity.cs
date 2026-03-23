namespace CephasOps.Domain.Common;

/// <summary>
/// Base entity class for all company-scoped entities
/// Includes soft delete support per documentation requirements
/// </summary>
public abstract class CompanyScopedEntity
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Company ID this entity belongs to (nullable since company feature removed)
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Timestamp when the entity was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the entity was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft delete flag - when true, entity is considered deleted
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Timestamp when the entity was soft deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User ID who deleted the entity
    /// </summary>
    public Guid? DeletedByUserId { get; set; }

    /// <summary>
    /// Concurrency token for optimistic concurrency control
    /// </summary>
    public byte[]? RowVersion { get; set; }
}

