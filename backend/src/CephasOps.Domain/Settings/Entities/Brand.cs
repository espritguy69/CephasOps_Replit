using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// Brand Entity
/// Represents equipment and material brands (Huawei, TP-Link, Cisco, etc.)
/// </summary>
public class Brand : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Brand details
    public string? Country { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    
    // Metrics
    public int MaterialCount { get; set; } = 0;
    
    // Status
    public bool IsActive { get; set; } = true;
    
    // Audit
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

