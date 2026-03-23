namespace CephasOps.Application.Orders.DTOs;

/// <summary>
/// OrderType DTO (parent or subtype)
/// </summary>
public class OrderTypeDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ParentOrderTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    /// <summary>Number of subtypes when this is a parent; 0 for subtypes.</summary>
    public int ChildCount { get; set; }
}

/// <summary>
/// Create OrderType request DTO (parent or subtype when ParentOrderTypeId is set)
/// </summary>
public class CreateOrderTypeDto
{
    public Guid? DepartmentId { get; set; }
    public Guid? ParentOrderTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Update OrderType request DTO
/// </summary>
public class UpdateOrderTypeDto
{
    public Guid? DepartmentId { get; set; }
    public Guid? ParentOrderTypeId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public int? DisplayOrder { get; set; }
}

