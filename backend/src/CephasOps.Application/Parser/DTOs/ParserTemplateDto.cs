namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// Parser template DTO
/// </summary>
public class ParserTemplateDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? EmailAccountId { get; set; }
    public string? EmailAccountName { get; set; }
    public string? PartnerPattern { get; set; }
    public string? SubjectPattern { get; set; }
    public Guid? OrderTypeId { get; set; }
    public string? OrderTypeCode { get; set; }
    public bool AutoApprove { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? DefaultDepartmentId { get; set; }
    public string? ExpectedAttachmentTypes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create parser template request DTO
/// </summary>
public class CreateParserTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? EmailAccountId { get; set; }
    public string? PartnerPattern { get; set; }
    public string? SubjectPattern { get; set; }
    public Guid? OrderTypeId { get; set; }
    public string? OrderTypeCode { get; set; }
    public bool AutoApprove { get; set; } = false;
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? DefaultDepartmentId { get; set; }
    public string? ExpectedAttachmentTypes { get; set; }
}

/// <summary>
/// Update parser template request DTO
/// </summary>
public class UpdateParserTemplateDto
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public Guid? EmailAccountId { get; set; }
    public string? PartnerPattern { get; set; }
    public string? SubjectPattern { get; set; }
    public Guid? OrderTypeId { get; set; }
    public string? OrderTypeCode { get; set; }
    public bool? AutoApprove { get; set; }
    public int? Priority { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? DefaultDepartmentId { get; set; }
    public string? ExpectedAttachmentTypes { get; set; }
}

