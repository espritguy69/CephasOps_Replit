namespace CephasOps.Application.Rates.DTOs;

/// <summary>
/// Mapping of an order category to a service profile.
/// </summary>
public class OrderCategoryServiceProfileDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid OrderCategoryId { get; set; }
    public string? OrderCategoryName { get; set; }
    public string? OrderCategoryCode { get; set; }
    public Guid ServiceProfileId { get; set; }
    public string? ServiceProfileName { get; set; }
    public string? ServiceProfileCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateOrderCategoryServiceProfileDto
{
    public Guid OrderCategoryId { get; set; }
    public Guid ServiceProfileId { get; set; }
}

public class OrderCategoryServiceProfileListFilter
{
    public Guid? ServiceProfileId { get; set; }
    public Guid? OrderCategoryId { get; set; }
}
