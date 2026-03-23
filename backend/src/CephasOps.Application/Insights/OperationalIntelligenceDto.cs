namespace CephasOps.Application.Insights;

/// <summary>Explainable reason for a risk signal. Every flag or score must show why it was raised.</summary>
public class IntelligenceExplanationDto
{
    public string RuleCode { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public int? SourceCount { get; set; }
    public string Severity { get; set; } = "Warning"; // Info | Warning | Critical
}

/// <summary>Installer risk signal: repeated blockers, high replacement rate, stuck orders, reschedules, high issue ratio vs peers.</summary>
public class InstallerRiskSignalDto
{
    public Guid InstallerId { get; set; }
    public string? InstallerDisplayName { get; set; }
    public Guid CompanyId { get; set; }
    public string Severity { get; set; } = "Warning";
    public DateTime DetectedAtUtc { get; set; }
    public IReadOnlyList<IntelligenceExplanationDto> Reasons { get; set; } = new List<IntelligenceExplanationDto>();
}

/// <summary>Building/site risk signal: repeated blockers, replacements, completion failures, status loops at same site.</summary>
public class BuildingRiskSignalDto
{
    public Guid BuildingId { get; set; }
    public string? BuildingDisplayName { get; set; }
    public Guid CompanyId { get; set; }
    public string Severity { get; set; } = "Warning";
    public DateTime DetectedAtUtc { get; set; }
    public IReadOnlyList<IntelligenceExplanationDto> Reasons { get; set; } = new List<IntelligenceExplanationDto>();
}

/// <summary>Order risk signal: likely stuck, already stuck, reschedule-heavy, blocker accumulation, replacement-heavy, silent.</summary>
public class OrderRiskSignalDto
{
    public Guid OrderId { get; set; }
    public string? OrderRef { get; set; }
    public Guid CompanyId { get; set; }
    public string? Status { get; set; }
    public Guid? AssignedSiId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string Severity { get; set; } = "Warning";
    public DateTime DetectedAtUtc { get; set; }
    public IReadOnlyList<IntelligenceExplanationDto> Reasons { get; set; } = new List<IntelligenceExplanationDto>();
}

/// <summary>Tenant-level operational risk: spike in stuck/blocker-heavy orders, abnormal replacement ratio, installer issue cluster, health deterioration.</summary>
public class TenantRiskSignalDto
{
    public Guid CompanyId { get; set; }
    public Guid? TenantId { get; set; }
    public string Severity { get; set; } = "Warning";
    public DateTime DetectedAtUtc { get; set; }
    public IReadOnlyList<IntelligenceExplanationDto> Reasons { get; set; } = new List<IntelligenceExplanationDto>();
}

/// <summary>SLA/delay risk: order nearing breach, overdue in status, installer inactivity likely to cause breach.</summary>
public class SlaDelayRiskDto
{
    public Guid OrderId { get; set; }
    public string? OrderRef { get; set; }
    public Guid CompanyId { get; set; }
    public string Severity { get; set; } = "Warning";
    public DateTime DetectedAtUtc { get; set; }
    public IReadOnlyList<IntelligenceExplanationDto> Reasons { get; set; } = new List<IntelligenceExplanationDto>();
}

/// <summary>Summary of operational intelligence for a tenant or platform.</summary>
public class OperationalIntelligenceSummaryDto
{
    public int OrdersAtRiskCount { get; set; }
    public int InstallersAtRiskCount { get; set; }
    public int BuildingsAtRiskCount { get; set; }
    public int CriticalCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
}
