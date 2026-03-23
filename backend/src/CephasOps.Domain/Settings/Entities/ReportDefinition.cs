using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

public class ReportDefinition : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Format { get; set; } = "PDF";
    public string? Schedule { get; set; }
    public DateTime? LastGenerated { get; set; }
    public bool IsActive { get; set; } = true;
}

