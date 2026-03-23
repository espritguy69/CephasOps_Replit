namespace CephasOps.Application.Settings.DTOs;

public class ReportDefinitionDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string? Schedule { get; set; }
    public DateTime? LastGenerated { get; set; }
    public bool IsActive { get; set; }
}

