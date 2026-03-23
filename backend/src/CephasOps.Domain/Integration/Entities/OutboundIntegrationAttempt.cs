namespace CephasOps.Domain.Integration.Entities;

/// <summary>
/// One HTTP (or provider) attempt for an outbound delivery. Full history for diagnostics.
/// </summary>
public class OutboundIntegrationAttempt
{
    public Guid Id { get; set; }

    public Guid OutboundDeliveryId { get; set; }
    public OutboundIntegrationDelivery? OutboundDelivery { get; set; }

    public int AttemptNumber { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public bool Success { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ResponseBodySnippet { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Request duration in milliseconds.</summary>
    public int? DurationMs { get; set; }
}
