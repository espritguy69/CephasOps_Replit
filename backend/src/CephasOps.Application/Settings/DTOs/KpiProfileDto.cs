namespace CephasOps.Application.Settings.DTOs;

/// <summary>
/// DTO for KPI profile
/// </summary>
public class KpiProfileDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Name { get; set; } = string.Empty;
    public Guid? PartnerId { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public Guid? BuildingTypeId { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public int MaxJobDurationMinutes { get; set; }
    public int DocketKpiMinutes { get; set; }
    public int? MaxReschedulesAllowed { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating KPI profile
/// </summary>
public class CreateKpiProfileDto
{
    public string Name { get; set; } = string.Empty;
    public Guid? PartnerId { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public Guid? BuildingTypeId { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public int MaxJobDurationMinutes { get; set; }
    public int DocketKpiMinutes { get; set; }
    public int? MaxReschedulesAllowed { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>
/// DTO for updating KPI profile
/// </summary>
public class UpdateKpiProfileDto
{
    public string? Name { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public int? MaxJobDurationMinutes { get; set; }
    public int? DocketKpiMinutes { get; set; }
    public int? MaxReschedulesAllowed { get; set; }
    public bool? IsDefault { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>
/// DTO for KPI evaluation result
/// </summary>
public class KpiEvaluationResultDto
{
    public Guid OrderId { get; set; }
    public Guid? KpiProfileId { get; set; }
    public string KpiResult { get; set; } = string.Empty; // OnTime, Late, ExceededSla
    public int ActualJobMinutes { get; set; }
    public int TargetJobMinutes { get; set; }
    public int? ActualDocketMinutes { get; set; }
    public int TargetDocketMinutes { get; set; }
    public int DeltaMinutes { get; set; }
}

