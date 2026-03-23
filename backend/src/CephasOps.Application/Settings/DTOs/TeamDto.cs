namespace CephasOps.Application.Settings.DTOs;

public class TeamDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? TeamLeaderId { get; set; }
    public string? TeamLeaderName { get; set; }
    public int MemberCount { get; set; }
    public int ActiveJobsCount { get; set; }
    public bool IsActive { get; set; }
}

public class CreateTeamDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? TeamLeaderId { get; set; }
}

public class UpdateTeamDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? TeamLeaderId { get; set; }
    public bool IsActive { get; set; }
}

