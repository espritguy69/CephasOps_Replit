namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// Email rule DTO
/// </summary>
public class EmailRuleDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? EmailAccountId { get; set; }
    public string? FromAddressPattern { get; set; }
    public string? DomainPattern { get; set; }
    public string? SubjectContains { get; set; }
    public bool IsVip { get; set; }
    public Guid? TargetDepartmentId { get; set; }
    public Guid? TargetUserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create email rule request DTO
/// </summary>
public class CreateEmailRuleDto
{
    public Guid? EmailAccountId { get; set; }
    public string? FromAddressPattern { get; set; }
    public string? DomainPattern { get; set; }
    public string? SubjectContains { get; set; }
    public bool IsVip { get; set; }
    public Guid? TargetDepartmentId { get; set; }
    public Guid? TargetUserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Update email rule request DTO
/// </summary>
public class UpdateEmailRuleDto
{
    public Guid? EmailAccountId { get; set; }
    public string? FromAddressPattern { get; set; }
    public string? DomainPattern { get; set; }
    public string? SubjectContains { get; set; }
    public bool? IsVip { get; set; }
    public Guid? TargetDepartmentId { get; set; }
    public Guid? TargetUserId { get; set; }
    public string? ActionType { get; set; }
    public int? Priority { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}

