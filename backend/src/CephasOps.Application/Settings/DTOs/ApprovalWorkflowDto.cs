namespace CephasOps.Application.Settings.DTOs;

/// <summary>
/// DTO for Approval Workflow
/// </summary>
public class ApprovalWorkflowDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string EntityType { get; set; } = "Order";
    public Guid? PartnerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? OrderType { get; set; }
    public decimal? MinValueThreshold { get; set; }
    public bool RequireAllSteps { get; set; } = true;
    public bool AllowParallelApproval { get; set; } = false;
    public int? TimeoutMinutes { get; set; }
    public bool AutoApproveOnTimeout { get; set; } = false;
    public string? EscalationRole { get; set; }
    public Guid? EscalationUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ApprovalStepDto> Steps { get; set; } = new();
}

/// <summary>
/// DTO for Approval Step
/// </summary>
public class ApprovalStepDto
{
    public Guid Id { get; set; }
    public Guid ApprovalWorkflowId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string ApprovalType { get; set; } = string.Empty;
    public Guid? TargetUserId { get; set; }
    public string? TargetRole { get; set; }
    public Guid? TargetTeamId { get; set; }
    public string? ExternalSource { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool CanSkipIfPreviousApproved { get; set; } = false;
    public int? TimeoutMinutes { get; set; }
    public bool AutoApproveOnTimeout { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating Approval Workflow
/// </summary>
public class CreateApprovalWorkflowDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string EntityType { get; set; } = "Order";
    public Guid? PartnerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? OrderType { get; set; }
    public decimal? MinValueThreshold { get; set; }
    public bool RequireAllSteps { get; set; } = true;
    public bool AllowParallelApproval { get; set; } = false;
    public int? TimeoutMinutes { get; set; }
    public bool AutoApproveOnTimeout { get; set; } = false;
    public string? EscalationRole { get; set; }
    public Guid? EscalationUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public List<CreateApprovalStepDto> Steps { get; set; } = new();
}

/// <summary>
/// DTO for creating Approval Step
/// </summary>
public class CreateApprovalStepDto
{
    public string Name { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string ApprovalType { get; set; } = string.Empty;
    public Guid? TargetUserId { get; set; }
    public string? TargetRole { get; set; }
    public Guid? TargetTeamId { get; set; }
    public string? ExternalSource { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool CanSkipIfPreviousApproved { get; set; } = false;
    public int? TimeoutMinutes { get; set; }
    public bool AutoApproveOnTimeout { get; set; } = false;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for updating Approval Workflow
/// </summary>
public class UpdateApprovalWorkflowDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? MinValueThreshold { get; set; }
    public bool? RequireAllSteps { get; set; }
    public bool? AllowParallelApproval { get; set; }
    public int? TimeoutMinutes { get; set; }
    public bool? AutoApproveOnTimeout { get; set; }
    public string? EscalationRole { get; set; }
    public Guid? EscalationUserId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDefault { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

