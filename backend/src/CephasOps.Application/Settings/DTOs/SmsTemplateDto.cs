namespace CephasOps.Application.Settings.DTOs;

/// <summary>
/// SMS template data transfer object
/// </summary>
public class SmsTemplateDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public int CharCount { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create SMS template DTO
/// </summary>
public class CreateSmsTemplateDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

/// <summary>
/// Update SMS template DTO
/// </summary>
public class UpdateSmsTemplateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? MessageText { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}

