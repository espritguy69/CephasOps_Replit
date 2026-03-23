namespace CephasOps.Application.Settings.DTOs;

/// <summary>
/// DTO for global setting
/// </summary>
public class GlobalSettingDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = "String";
    public string? Description { get; set; }
    public string? Module { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating/updating global setting
/// </summary>
public class CreateGlobalSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = "String";
    public string? Description { get; set; }
    public string? Module { get; set; }
}

/// <summary>
/// DTO for updating global setting value
/// </summary>
public class UpdateGlobalSettingDto
{
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}

