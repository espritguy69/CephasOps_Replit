using CephasOps.Domain.Pnl.Enums;

namespace CephasOps.Application.Pnl.DTOs;

/// <summary>
/// PnL Type DTO
/// </summary>
public class PnlTypeDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PnlTypeCategory Category { get; set; }
    public string CategoryName => Category.ToString();
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsTransactional { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PnlTypeDto> Children { get; set; } = new();
}

/// <summary>
/// Create PnL Type request DTO
/// </summary>
public class CreatePnlTypeDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PnlTypeCategory Category { get; set; }
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsTransactional { get; set; } = true;
}

/// <summary>
/// Update PnL Type request DTO
/// </summary>
public class UpdatePnlTypeDto
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsTransactional { get; set; }
}

/// <summary>
/// PnL Type tree node for hierarchical display
/// </summary>
public class PnlTypeTreeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PnlTypeCategory Category { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsTransactional { get; set; }
    public int Level { get; set; }
    public List<PnlTypeTreeDto> Children { get; set; } = new();
}

