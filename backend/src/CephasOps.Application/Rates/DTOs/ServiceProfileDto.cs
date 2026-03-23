namespace CephasOps.Application.Rates.DTOs;

/// <summary>
/// Service profile DTO — groups order categories for pricing (e.g. RESIDENTIAL_FIBER, BUSINESS_FIBER).
/// </summary>
public class ServiceProfileDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateServiceProfileDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

public class UpdateServiceProfileDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public int? DisplayOrder { get; set; }
}

public class ServiceProfileListFilter
{
    public bool? IsActive { get; set; }
    public string? Search { get; set; }
}
