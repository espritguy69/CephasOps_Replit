namespace CephasOps.Application.Sla.DTOs;

public class SlaBreachDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid RuleId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime DetectedAtUtc { get; set; }
    public double DurationSeconds { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}

public class SlaDashboardDto
{
    public int OpenBreachesCount { get; set; }
    public int CriticalBreachesCount { get; set; }
    public double? AverageResolutionTimeHours { get; set; }
    public List<SlaBreachSummaryByTargetDto> MostCommonBreachedTargets { get; set; } = new();
}

public class SlaBreachSummaryByTargetDto
{
    public string TargetType { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public int Count { get; set; }
}
