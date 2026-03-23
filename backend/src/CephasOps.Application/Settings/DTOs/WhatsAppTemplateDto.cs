namespace CephasOps.Application.Settings.DTOs;

/// <summary>
/// WhatsApp template data transfer object
/// </summary>
public class WhatsAppTemplateDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? TemplateId { get; set; }
    public string ApprovalStatus { get; set; } = "Pending";
    public string? MessageBody { get; set; }
    public string? Language { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create WhatsApp template DTO
/// </summary>
public class CreateWhatsAppTemplateDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? TemplateId { get; set; }
    public string ApprovalStatus { get; set; } = "Pending";
    public string? MessageBody { get; set; }
    public string? Language { get; set; } = "en";
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

/// <summary>
/// Update WhatsApp template DTO
/// </summary>
public class UpdateWhatsAppTemplateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? TemplateId { get; set; }
    public string? ApprovalStatus { get; set; }
    public string? MessageBody { get; set; }
    public string? Language { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}

