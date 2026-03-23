namespace CephasOps.Domain.Commands;

/// <summary>
/// Log of command execution for idempotency: one successful completion per idempotency key.
/// </summary>
public class CommandProcessingLog
{
    public Guid Id { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;
    public string CommandType { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public Guid? WorkflowInstanceId { get; set; }

    /// <summary>Pending | Completed | Failed</summary>
    public string Status { get; set; } = Statuses.Pending;

    public string? ResultJson { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public static class Statuses
    {
        public const string Pending = "Pending";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }
}
