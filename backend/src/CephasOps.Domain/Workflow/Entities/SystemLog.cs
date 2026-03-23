namespace CephasOps.Domain.Workflow.Entities;

/// <summary>
/// System log entity - application-level structured logging
/// </summary>
public class SystemLog
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Company ID (nullable for global events)
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Severity level (Info, Warning, Error, Critical)
    /// </summary>
    public SystemLogSeverity Severity { get; set; } = SystemLogSeverity.Info;

    /// <summary>
    /// Category/Module (e.g., "Orders", "Billing", "Parser", "Workflow")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Short description message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional structured information in JSON format
    /// </summary>
    public string? DetailsJson { get; set; }

    /// <summary>
    /// User ID who triggered the log entry (if applicable)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Entity type related to this log entry (e.g., "Order", "Invoice")
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Entity ID related to this log entry
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Timestamp when the log entry was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Enumeration of system log severity levels
/// </summary>
public enum SystemLogSeverity
{
    /// <summary>
    /// Informational message
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning message
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error message
    /// </summary>
    Error = 2,

    /// <summary>
    /// Critical error message
    /// </summary>
    Critical = 3
}

