namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// Email template DTO
/// </summary>
public class EmailTemplateDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? EmailAccountId { get; set; }
    public string? EmailAccountName { get; set; }
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? RelatedEntityType { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public bool AutoProcessReplies { get; set; }
    public string? ReplyPattern { get; set; }
    public string? Description { get; set; }
    public string Direction { get; set; } = "Outgoing";
    public Guid CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create email template request DTO
/// </summary>
public class CreateEmailTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? EmailAccountId { get; set; }
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? RelatedEntityType { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AutoProcessReplies { get; set; } = false;
    public string? ReplyPattern { get; set; }
    public string? Description { get; set; }
    public string Direction { get; set; } = "Outgoing";
}

/// <summary>
/// Update email template request DTO
/// </summary>
public class UpdateEmailTemplateDto
{
    public string? Name { get; set; }
    public string? SubjectTemplate { get; set; }
    public string? BodyTemplate { get; set; }
    public Guid? EmailAccountId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? Priority { get; set; }
    public bool? IsActive { get; set; }
    public bool? AutoProcessReplies { get; set; }
    public string? ReplyPattern { get; set; }
    public string? Description { get; set; }
    public string? Direction { get; set; }
}

/// <summary>
/// Email sending result DTO
/// </summary>
public class EmailSendingResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid EmailAccountId { get; set; }
    public Guid? EmailMessageId { get; set; }
    public string? MessageId { get; set; }
}

/// <summary>
/// Send email request DTO
/// </summary>
public class SendEmailDto
{
    public Guid EmailAccountId { get; set; }
    public Guid? EmailTemplateId { get; set; }
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public Dictionary<string, string>? Placeholders { get; set; } // For template variable replacement
}

/// <summary>
/// Send reschedule request DTO
/// </summary>
public class SendRescheduleRequestDto
{
    public Guid OrderId { get; set; }
    public Guid? EmailTemplateId { get; set; } // Optional: use specific template, otherwise auto-select
    public DateTime NewDate { get; set; }
    public TimeSpan NewWindowFrom { get; set; }
    public TimeSpan NewWindowTo { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? EmailAccountId { get; set; }
    public string? RescheduleType { get; set; } // "TimeOnly", "DateAndTime", "Assurance"
}

