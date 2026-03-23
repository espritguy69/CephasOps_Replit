namespace CephasOps.Application.Buildings.DTOs;

/// <summary>
/// Splitter port DTO
/// </summary>
public class SplitterPortDto
{
    public Guid Id { get; set; }
    public Guid SplitterId { get; set; }
    public int PortNumber { get; set; }
    public string Status { get; set; } = string.Empty; // Available, Used, Reserved, Standby
    public Guid? OrderId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public bool IsStandby { get; set; }
    
    /// <summary>
    /// True if standby port override was approved (for standby ports being used)
    /// </summary>
    public bool StandbyOverrideApproved { get; set; }
    
    /// <summary>
    /// Approval attachment ID (required when using standby port)
    /// </summary>
    public Guid? ApprovalAttachmentId { get; set; }
}

/// <summary>
/// Splitter DTO
/// </summary>
public class SplitterDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public Guid BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public Guid? SplitterTypeId { get; set; } // FK to SplitterType entity
    public string? Location { get; set; }
    public string? Block { get; set; }
    public string? Floor { get; set; }
    public Guid? DepartmentId { get; set; } // Optional: department assignment
    public bool IsActive { get; set; }
    public List<SplitterPortDto> Ports { get; set; } = new();
    public int TotalPorts => Ports.Count;
    public int UsedPorts => Ports.Count(p => p.Status == "Used");
    public int AvailablePorts => Ports.Count(p => p.Status == "Available");
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Create splitter request DTO
/// </summary>
public class CreateSplitterDto
{
    public Guid? CompanyId { get; set; } // Optional: if not provided, use from user context
    public Guid? DepartmentId { get; set; } // Optional: department assignment
    public Guid BuildingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public Guid? SplitterTypeId { get; set; } // FK to SplitterType entity
    public string? Location { get; set; }
    public string? Block { get; set; }
    public string? Floor { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Update splitter request DTO
/// </summary>
public class UpdateSplitterDto
{
    public Guid? DepartmentId { get; set; } // Optional: department assignment
    public string? Name { get; set; }
    public string? Code { get; set; }
    public Guid? SplitterTypeId { get; set; } // FK to SplitterType entity
    public string? Location { get; set; }
    public string? Block { get; set; }
    public string? Floor { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Update splitter port request DTO
/// </summary>
public class UpdateSplitterPortDto
{
    public string Status { get; set; } = string.Empty; // Available, Used, Reserved, Standby
    public Guid? OrderId { get; set; }
    
    /// <summary>
    /// Approval attachment ID - REQUIRED when setting a standby port (port 32 on 1:32 splitters) to "Used"
    /// Per SPLITTERS_MANAGEMENT_MODULE.md: Standby port usage requires prior partner approval
    /// </summary>
    public Guid? ApprovalAttachmentId { get; set; }
}

