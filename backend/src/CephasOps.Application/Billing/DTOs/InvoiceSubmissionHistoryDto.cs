namespace CephasOps.Application.Billing.DTOs;

/// <summary>
/// Invoice submission history DTO
/// </summary>
public class InvoiceSubmissionHistoryDto
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string SubmissionId { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ResponseMessage { get; set; }
    public string? ResponseCode { get; set; }
    public string? RejectionReason { get; set; }
    public string PortalType { get; set; } = string.Empty;
    public Guid SubmittedByUserId { get; set; }
    public string? SubmittedByUserName { get; set; }
    public bool IsActive { get; set; }
    public string? PaymentStatus { get; set; }
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

