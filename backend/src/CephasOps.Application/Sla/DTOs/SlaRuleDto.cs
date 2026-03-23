namespace CephasOps.Application.Sla.DTOs;

public class SlaRuleDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public int MaxDurationSeconds { get; set; }
    public int? WarningThresholdSeconds { get; set; }
    public int? EscalationThresholdSeconds { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateSlaRuleDto
{
    public Guid? CompanyId { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public int MaxDurationSeconds { get; set; }
    public int? WarningThresholdSeconds { get; set; }
    public int? EscalationThresholdSeconds { get; set; }
    public bool Enabled { get; set; } = true;
}

public class UpdateSlaRuleDto
{
    public string? RuleType { get; set; }
    public string? TargetType { get; set; }
    public string? TargetName { get; set; }
    public int? MaxDurationSeconds { get; set; }
    public int? WarningThresholdSeconds { get; set; }
    public int? EscalationThresholdSeconds { get; set; }
    public bool? Enabled { get; set; }
}
