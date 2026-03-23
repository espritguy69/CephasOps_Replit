namespace CephasOps.Application.Orders.DTOs;

/// <summary>
/// DTO for OrderStatusChecklistItem
/// </summary>
public class OrderStatusChecklistItemDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public Guid? ParentChecklistItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Nested sub-steps (for hierarchical display)
    public List<OrderStatusChecklistItemDto> SubSteps { get; set; } = new();
}

/// <summary>
/// DTO for creating a checklist item
/// </summary>
public class CreateOrderStatusChecklistItemDto
{
    public string StatusCode { get; set; } = string.Empty;
    public Guid? ParentChecklistItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for updating a checklist item
/// </summary>
public class UpdateOrderStatusChecklistItemDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? OrderIndex { get; set; }
    public bool? IsRequired { get; set; }
    public bool? IsActive { get; set; }
}

