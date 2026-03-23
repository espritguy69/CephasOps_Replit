namespace CephasOps.Application.Agent.DTOs;

/// <summary>
/// Agent processing result
/// </summary>
public class AgentProcessingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string Action { get; set; } = string.Empty; // e.g., "EmailReplyProcessed", "PaymentRejected", "RescheduleApproved"
    public Guid? EntityId { get; set; }
    public string? EntityType { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Agent routing result
/// </summary>
public class AgentRoutingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid OrderId { get; set; }
    public Guid? RecommendedDepartmentId { get; set; }
    public Guid? RecommendedSiId { get; set; }
    public string? RoutingReason { get; set; }
    public decimal ConfidenceScore { get; set; } // 0.0 to 1.0
    public Dictionary<string, object>? RoutingFactors { get; set; }
}

/// <summary>
/// Agent KPI calculation result
/// </summary>
public class AgentKpiResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Dictionary<string, decimal> Metrics { get; set; } = new();
    public Dictionary<string, object>? Insights { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Payment rejection DTO
/// </summary>
public class PaymentRejectionDto
{
    public Guid PaymentId { get; set; }
    public Guid InvoiceId { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public string? RejectionCode { get; set; }
    public DateTime RejectedAt { get; set; } = DateTime.UtcNow;
    public string? SubmissionId { get; set; } // Original submission ID for reference
}

