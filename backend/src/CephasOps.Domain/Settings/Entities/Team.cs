using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// Team Entity
/// Represents work teams (SI teams, project teams, etc.)
/// </summary>
public class Team : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Department association
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    
    // Team leadership
    public Guid? TeamLeaderId { get; set; }
    public string? TeamLeaderName { get; set; }
    
    // Metrics
    public int MemberCount { get; set; } = 0;
    public int ActiveJobsCount { get; set; } = 0;
    
    // Status
    public bool IsActive { get; set; } = true;
    
    // Audit
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

