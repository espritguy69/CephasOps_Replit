namespace CephasOps.Application.Orders.DTOs;

/// <summary>
/// OrderCategory DTO - represents service/order categories (FTTH, FTTO, FTTR, FTTC)
/// Previously known as InstallationType but renamed for clarity.
/// </summary>
public class OrderCategoryDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create OrderCategory request DTO
/// </summary>
public class CreateOrderCategoryDto
{
    public Guid? DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Update OrderCategory request DTO
/// </summary>
public class UpdateOrderCategoryDto
{
    public Guid? DepartmentId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public int? DisplayOrder { get; set; }
}

