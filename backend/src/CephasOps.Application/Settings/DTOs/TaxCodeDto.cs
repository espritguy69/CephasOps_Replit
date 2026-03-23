namespace CephasOps.Application.Settings.DTOs;

public class TaxCodeDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateTaxCodeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
}

public class UpdateTaxCodeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? TaxRate { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsActive { get; set; }
}
